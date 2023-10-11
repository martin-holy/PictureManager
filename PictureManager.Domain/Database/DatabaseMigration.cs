using MH.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PictureManager.Domain.Database;

public static class DatabaseMigration {
  public static void Resolver(int oldVersion, int newVersion) {
    if (oldVersion < 1) From0To1();
    if (oldVersion < 2) From1To2();
    if (oldVersion < 3) From2To3();
    if (oldVersion < 4) From3To4();
    if (oldVersion < 5) From4To5();
    if (oldVersion < 6) From5To6();
  }

  /// <summary>
  /// FavoriteFolders
  ///   old => ID|Folder
  ///   new => ID|Folder|Title
  /// Keywords
  ///   old => ID|Name|Parent|Index
  ///   new => ID|Name|Parent
  /// People
  ///   old => ID|Name
  ///   new => ID|Name|Segments|Keywords
  /// Viewers
  ///   old => ID|Name|IncludedFolders|ExcludedFolders|IsDefault
  ///   new => ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
  /// </summary>
  private static void From0To1() {
    SimpleDB.MigrateFile(
      Core.Db.FavoriteFolders.TableFilePath,
      record => $"{record}|Favorite folder name");

    SimpleDB.MigrateFile(
      Core.Db.Keywords.TableFilePath,
      record => record[..record.LastIndexOf('|')]);

    SimpleDB.MigrateFile(
      Core.Db.People.TableFilePath,
      record => $"{record}||");

    SimpleDB.MigrateFile(
      Core.Db.Viewers.TableFilePath,
      record => {
        var lio = record.LastIndexOf('|');
        return $"{record[..lio]}||{record[lio..]}";
      });
  }

  /// <summary>
  /// FavoriteFolders, Folders, MediaItems, Segments, VideoClips and VideoClipsGroups
  /// are stored in separate files for each drive
  /// </summary>
  private static void From1To2() {
    Core.Db.FavoriteFolders.IsModified = true;
    Core.Db.Folders.IsModified = true;
    Core.Db.MediaItems.IsModified = true;
    Core.Db.Segments.IsModified = true;
    Core.Db.VideoClips.IsModified = true;
    Core.Db.VideoClipsGroups.IsModified = true;
  }

  /// <summary>
  /// Segments
  ///   old => ID|MediaItemId|PersonId|SegmentBox(centerX,centerY,radius)|Keywords
  ///   new => ID|MediaItemId|PersonId|SegmentBox(left,top,size)|Keywords
  /// </summary>
  private static void From2To3() {
    var files = Directory.GetFiles("db")
      .Where(x => x.StartsWith("db" + Path.DirectorySeparatorChar + "Segments."));

    foreach (var file in files) {
      SimpleDB.MigrateFile(
        file,
        record => {
          var values = record.Split('|');
          var oldRect = values[3].Split(',');
          var x = int.Parse(oldRect[0]);
          var y = int.Parse(oldRect[1]);
          var r = int.Parse(oldRect[2]);
          var newRect = string.Join(',', x - r, y - r, r * 2);

          return string.Join('|', values[0], values[1], values[2], newRect, values[4]);
        });
    }
  }

  /// <summary>
  /// Folders
  ///   old => ID|Name|Parent|IsFolderKeyword
  ///   new => ID|Name|Parent
  /// FolderKeywords
  ///   new => ID
  /// </summary>
  private static void From3To4() {
    var fks = new List<string>();
    var files = Directory.GetFiles("db")
      .Where(x => x.StartsWith("db" + Path.DirectorySeparatorChar + "Folders."));

    foreach (var file in files) {
      fks.Clear();

      SimpleDB.MigrateFile(
        file,
        record => {
          var values = record.Split('|');

          if (values[3] == "1")
            fks.Add(values[0]);

          return string.Join('|', values[0], values[1], values[2]);
        });

      if (fks.Count == 0) continue;

      using var sw = new StreamWriter(file.Replace("Folders", "FolderKeywords"), false, Encoding.UTF8, 65536);
          
      foreach (var fk in fks)
        sw.WriteLine(fk);

      sw.Close();
    }
  }

  /// <summary>
  /// Add unknown people to UnknownGroup
  /// </summary>
  private static void From4To5() {
    Core.Db.ReadyEvent += (_, _) => {
      Core.PeopleM.TreeCategory.UnknownGroup.AddItems(
        Core.Db.People.All
          .Where(x => x.IsUnknown && x.Parent == null)
          .OrderBy(x => x.Name));
    };
  }

  /// <summary>
  /// Change unknown person id from negative to positive.
  /// Identification of unknown person will be Name starting with "P -"
  /// </summary>
  private static void From5To6() {
    var pIds = new List<int>();

    if (!SimpleDB.LoadFromFile(x => {
          pIds.Add(int.Parse(x.Split("|")[0]));
        }, Core.Db.People.TableFilePath)) return;

    var pIdsNeg = pIds.Where(x => x < 0).ToHashSet();
    var pIdsPos = pIds.Where(x => x > 0).ToHashSet();
    var pIdsDic = new Dictionary<int, int>();

    foreach (var pIdNeg in pIdsNeg) {
      var pIdPos = SimpleDB.GetNextRecycledId(pIdsPos) ?? ++Core.Db.People.MaxId;
      pIdsPos.Add(pIdPos);
      pIdsDic.Add(pIdNeg, pIdPos);
    }

    string UpdateIds(string line, int index, Func<string[], bool> skipLine) {
      var vars = line.Split("|");

      if (skipLine?.Invoke(vars) == true
          || string.IsNullOrEmpty(vars[index])
          || !vars[index].Contains('-'))
        return line;

      var ids = vars[index].Split(",").Select(int.Parse).ToArray();
      for (int i = 0; i < ids.Length; i++)
        if (ids[i] < 0) {
          if (!pIdsDic.TryGetValue(ids[i], out var id)) {
            id = ++Core.Db.People.MaxId;
            pIdsDic.Add(ids[i], id);
          }

          ids[i] = id;
        }

      vars[index] = string.Join(",", ids);

      return string.Join("|", vars);
    }

    SimpleDB.MigrateFile(
      Core.Db.CategoryGroups.TableFilePath,
      x => UpdateIds(x, 3, vars => int.Parse(vars[2]) != (int)Category.People));

    foreach (var filePath in GetDriveRelatedTableFilePaths(Core.Db.MediaItems.TableName))
      SimpleDB.MigrateFile(filePath, x => UpdateIds(x, 9, null));

    SimpleDB.MigrateFile(Core.Db.People.TableFilePath, x => UpdateIds(x, 0, null));

    foreach (var filePath in GetDriveRelatedTableFilePaths(Core.Db.Segments.TableName))
      SimpleDB.MigrateFile(filePath, x => UpdateIds(x, 2, null));

    Core.Db.People.IsModified = true;
    Core.Db.SaveIdSequences();
  }

  private static IEnumerable<string> GetDriveRelatedTableFilePaths(string tableName) =>
    Directory.GetFiles("db")
      .Where(x => x.StartsWith("db" + Path.DirectorySeparatorChar + $"{tableName}."));
}