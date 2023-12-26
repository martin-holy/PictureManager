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
    if (oldVersion < 7) From6To7();
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
      Core.Db.FavoriteFolders.FilePath,
      record => $"{record}|Favorite folder name");

    SimpleDB.MigrateFile(
      Core.Db.Keywords.FilePath,
      record => record[..record.LastIndexOf('|')]);

    SimpleDB.MigrateFile(
      Core.Db.People.FilePath,
      record => $"{record}||");

    SimpleDB.MigrateFile(
      Core.Db.Viewers.FilePath,
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
    //Core.Db.MediaItems.IsModified = true;
    Core.Db.Segments.IsModified = true;
    Core.Db.VideoClips.IsModified = true;
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
      var p = Core.Db.People.All
          .Where(x => x.IsUnknown && x.Parent == null)
        .OrderBy(x => x.Name)
        .ToArray();
      if (p.Length > 0)
        Core.PeopleM.TreeCategory.UnknownGroup.AddItems(p);
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
        }, Core.Db.People.FilePath)) return;

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
      Core.Db.CategoryGroups.FilePath,
      x => UpdateIds(x, 3, vars => int.Parse(vars[2]) != (int)Category.People));

    foreach (var filePath in GetDriveRelatedTableFilePaths("MediaItems"))
      SimpleDB.MigrateFile(filePath, x => UpdateIds(x, 9, null));

    SimpleDB.MigrateFile(Core.Db.People.FilePath, x => UpdateIds(x, 0, null));

    foreach (var filePath in GetDriveRelatedTableFilePaths(Core.Db.Segments.Name))
      SimpleDB.MigrateFile(filePath, x => UpdateIds(x, 2, null));

    Core.Db.People.IsModified = true;
    Core.Db.SaveIdSequences();
  }

  private static IEnumerable<string> GetDriveRelatedTableFilePaths(string tableName) =>
    Directory.GetFiles("db")
      .Where(x => x.StartsWith("db" + Path.DirectorySeparatorChar + $"{tableName}."));

  /// <summary>
  /// Create GeoLocation for each GeoName
  /// Split MediaItems to Images and Videos
  /// Remove GeoName from MediaItems and create MediaItemGeoLocation relation
  /// Update VideoClips ids to use one id sequence for all MediaItems
  /// Store VideoClip Name in Comment
  /// Create Keyword for each VideoClipsGroup, add Keywords to VideoClips and delete VideoClipsGroups
  /// Migrate VideoClipsGroups to VideoItemsOrder with new VideoClips ids
  /// </summary>
  private static void From6To7() {
    // Create GeoLocation for each GeoName
    // all Lat and Lng are lost and need to be read from metadata again
    var geoLocId = 0;
    var geoNameGeoLocDic = new Dictionary<string, int>();
    var gnFilePath = Path.Combine("db", "GeoNames.csv");
    if (File.Exists(gnFilePath)) {
      using var gSr = new StreamReader(Path.Combine("db", "GeoNames.csv"), Encoding.UTF8);
      using var geoLocSw = new StreamWriter(Path.Combine("db", "GeoLocations.csv"), false, Encoding.UTF8, 65536);

      while (gSr.ReadLine() is { } line) {
        var geoNameId = line.Split("|")[0];
        geoLocId++;
        geoNameGeoLocDic.Add(geoNameId, geoLocId);
        geoLocSw.WriteLine(string.Join("|", geoLocId, null, null, geoNameId));
      }
      Core.Db.GeoLocations.MaxId = geoLocId;
    }

    // Split MediaItems to Images and Videos
    // Remove GeoName from MediaItems and create MediaItemGeoLocation relation
    string[] imgExts = { ".jpg", ".jpeg" };
    string vidExt = ".mp4";
    foreach (var miFilePath in GetDriveRelatedTableFilePaths("MediaItems")) {
      var imgFilePath = miFilePath.Replace("MediaItems", "Images");
      var vidFilePath = miFilePath.Replace("MediaItems", "Videos");
      var miGeoLocFilePath = miFilePath.Replace("MediaItems", "MediaItemGeoLocation");
      bool hasImages = false;
      bool hasVideos = false;
      bool hasMiGeoLoc = false;
      using var miSr = new StreamReader(miFilePath, Encoding.UTF8);
      using var imgSw = new StreamWriter(imgFilePath, false, Encoding.UTF8, 65536);
      using var vidSw = new StreamWriter(vidFilePath, false, Encoding.UTF8, 65536);
      using var miGeoLocSw = new StreamWriter(miGeoLocFilePath, false, Encoding.UTF8, 65536);

      while (miSr.ReadLine() is { } line) {
        var vars = line.Split('|').ToList();
        var fileName = vars[2];
        var geoName = vars[8];
        vars.RemoveAt(8);
        var newLine = string.Join("|", vars);

        if (imgExts.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase))) {
          imgSw.WriteLine(newLine);
          hasImages = true;
          MiToGeoLoc();
        }
        else if (fileName.EndsWith(vidExt, StringComparison.OrdinalIgnoreCase)) {
          vidSw.WriteLine(newLine);
          hasVideos = true;
          MiToGeoLoc();
        }

        continue;

        void MiToGeoLoc() {
          if (string.IsNullOrEmpty(geoName)) return;
          miGeoLocSw.WriteLine(string.Join("|", vars[0], geoNameGeoLocDic[geoName]));
          hasMiGeoLoc = true;
        }
      }

      miSr.Close();
      imgSw.Close();
      vidSw.Close();
      miGeoLocSw.Close();

      File.Delete(miFilePath);
      if (!hasImages) File.Delete(imgFilePath);
      if (!hasVideos) File.Delete(vidFilePath);
      if (!hasMiGeoLoc) File.Delete(miGeoLocFilePath);
    }

    // Get VideoClipsGroup names and Clips
    var nameIdClips = new Dictionary<string, Tuple<int, List<int>>>();
    foreach (var vcgFilePath in GetDriveRelatedTableFilePaths("VideoClipsGroups")) {
      using var vcgSr = new StreamReader(vcgFilePath, Encoding.UTF8);
      while (vcgSr.ReadLine() is { } line) {
        var vars = line.Split('|');
        if (string.IsNullOrEmpty(vars[3])) continue;
        var clips = vars[3].Split(',').Select(int.Parse);
        if (nameIdClips.TryGetValue(vars[1], out var kc))
          kc.Item2.AddRange(clips);
        else
          nameIdClips.Add(vars[1], new(++Core.Db.Keywords.MaxId, new(clips)));
      }

      vcgSr.Close();
      //File.Delete(vcgFilePath); // do not delete it yet!
    }

    // Create Keyword for each VideoClipsGroup
    using var kSw = new StreamWriter(Path.Combine("db", "Keywords.csv"), true, Encoding.UTF8, 65536);
    foreach (var nameIdClip in nameIdClips.OrderBy(x => x.Key))
      kSw.WriteLine(string.Join('|', nameIdClip.Value.Item1.ToString(), nameIdClip.Key, string.Empty));

    // Create CategoryGroup for VideoClipsGroups Keywords
    using var cgSw = new StreamWriter(Path.Combine("db", "CategoryGroups.csv"), true, Encoding.UTF8, 65536);
    cgSw.WriteLine(string.Join('|',
      ++Core.Db.CategoryGroups.MaxId,
      "VideoClipsGroups",
      (int)Category.Keywords,
      string.Join(',', nameIdClips.Select(x => x.Value.Item1))));

    // Update VideoClips ids to use one id sequence for all MediaItems and add Keywords
    // get max id from MediaItems
    int maxId = Core.Db.IdSequences.GetValueOrDefault("MediaItems", 0);
    var vcOldNewId = new Dictionary<int, int>();
    var vidVcDic = new Dictionary<int, List<int>>();

    // VideoClips (update Id) and add Keywords
    // Store Name in Comment
    foreach (var vcFilePath in GetDriveRelatedTableFilePaths("VideoClips"))
      SimpleDB.MigrateFile(vcFilePath, line => {
        var vars = line.Split("|").ToList();
        var oldId = int.Parse(vars[0]);
        var vidId = int.Parse(vars[1]);
        vcOldNewId.Add(oldId, ++maxId);
        vars[0] = maxId.ToString();
        
        // add keyword (VideoClipsGroup)
        if (nameIdClips.SingleOrDefault(x => x.Value.Item2.Contains(oldId)) is var kv && kv.Value?.Item1 is var kId)
          vars[10] = kId.ToString();

        if (vidVcDic.TryGetValue(vidId, out var vid))
          vid.Add(maxId);
        else
          vidVcDic.Add(vidId, new() { maxId });

        // store Name in Comment
        vars[8] = vars[4];
        vars.RemoveAt(4);

        return string.Join("|", vars);
      });

    // Migrate VideoClipsGroups to VideoItemsOrder with new VideoClips ids
    foreach (var vcgFilePath in GetDriveRelatedTableFilePaths("VideoClipsGroups")) {
      var vmioFilePath = vcgFilePath.Replace("VideoClipsGroups", "VideoItemsOrder");
      using var vcgSr = new StreamReader(vcgFilePath, Encoding.UTF8);
      using var vmioSw = new StreamWriter(vmioFilePath, false, Encoding.UTF8, 65536);

      var vidVidClipsDic = new Dictionary<int, List<int>>();

      while (vcgSr.ReadLine() is { } line) {
        var vars = line.Split('|').ToList();
        if (string.IsNullOrEmpty(vars[3])) continue;
        var newIds = vars[3].Split(",").Select(oldId => vcOldNewId[int.Parse(oldId)]);
        var vidId = int.Parse(vars[2]);
        if (vidVidClipsDic.TryGetValue(vidId, out var list))
          list.AddRange(newIds);
        else
          vidVidClipsDic.Add(vidId, new(newIds));
      }

      foreach (var vvc in vidVidClipsDic) {
        // add items not in groups
        if (vidVcDic.TryGetValue(vvc.Key, out var allVc))
          vvc.Value.AddRange(allVc.Except(vvc.Value).ToArray());

        vmioSw.WriteLine(string.Join("|", vvc.Key, string.Join(",", vvc.Value)));
      }

      vcgSr.Close();
      File.Delete(vcgFilePath);
    }

    // all MediaItems have one id sequence
    Core.Db.Images.MaxId = maxId;
    Core.Db.Videos.MaxId = maxId;
    Core.Db.VideoClips.MaxId = maxId;
    Core.Db.VideoImages.MaxId = maxId;
    Core.Db.SaveIdSequences();
  }
}