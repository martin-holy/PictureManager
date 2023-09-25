/* migration from version 0 to 1
FavoriteFolders
  old => ID|Folder
  new => ID|Folder|Title
Keywords
  old => ID|Name|Parent|Index
  new => ID|Name|Parent
People
  old => ID|Name
  new => ID|Name|Segments|Keywords
Viewers
  old => ID|Name|IncludedFolders|ExcludedFolders|IsDefault
  new => ID|Name|IncludedFolders|ExcludedFolders|ExcludedCategoryGroups|ExcludedKeywords|IsDefault
*/

/* migration from version 1 to 2
 FavoriteFolders, Folders, MediaItems, Segments, VideoClips and VideoClipsGroups
 are stored in separate files for each drive
 */

/* migration from version 2 to 3
Segments
  old => ID|MediaItemId|PersonId|SegmentBox(centerX,centerY,radius)|Keywords
  new => ID|MediaItemId|PersonId|SegmentBox(left,top,size)|Keywords
 */

/* migration from version 3 to 4
Folders
  old => ID|Name|Parent|IsFolderKeyword
  new => ID|Name|Parent
FolderKeywords
  new => ID
 */

using MH.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PictureManager.Domain.Database {
  public static class DatabaseMigration {
    public static void Resolver(int oldVersion, int newVersion) {
      // 0 => 1
      if (oldVersion < 1) {
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

      // 1 => 2
      if (oldVersion < 2) {
        Core.Db.FavoriteFolders.IsModified = true;
        Core.Db.Folders.IsModified = true;
        Core.Db.MediaItems.IsModified = true;
        Core.Db.Segments.IsModified = true;
        Core.Db.VideoClips.IsModified = true;
        Core.Db.VideoClipsGroups.IsModified = true;
      }

      // 2 => 3
      if (oldVersion < 3) {
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

      // 3 => 4
      if (oldVersion < 4) {
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
    }
  }
}
