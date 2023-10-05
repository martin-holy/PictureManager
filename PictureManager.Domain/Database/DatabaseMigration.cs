using MH.Utils;
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
  /// Delete unknown people with without segments.
  /// Add unknown people to UnknownGroup
  /// </summary>
  private static void From4To5() {
    Core.Db.ReadyEvent += (_, _) => {
      var allPeople = Core.Db.Segments.All
        .Where(x => x.Person?.Id < 0)
        .Select(x => x.Person)
        .Distinct()
        .ToHashSet();
      var toDelete = Core.Db.People.All
        .Where(x => x.Id < 0 && !allPeople.Contains(x))
        .ToArray();

      foreach (var person in toDelete)
        Core.Db.People.ItemDelete(person);

      Core.PeopleM.TreeCategory.UnknownGroup.AddItems(
        Core.Db.People.All
          .Where(x => x.Id < 0)
          .OrderBy(x => x.Name));
    };
  }
}