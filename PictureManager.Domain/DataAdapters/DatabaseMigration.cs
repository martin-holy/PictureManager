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

using MH.Utils;

namespace PictureManager.Domain.DataAdapters {
  public static class DatabaseMigration {
    public static void Resolver(int oldVersion, int newVersion) {
      var core = Core.Instance;

      // 0 => 1
      if (oldVersion == 0) {
        SimpleDB.MigrateFile(
          core.FavoriteFoldersM.DataAdapter.TableFilePath,
          record => $"{record}|Favorite folder name");

         SimpleDB.MigrateFile(
          core.KeywordsM.DataAdapter.TableFilePath,
          record => record[..record.LastIndexOf('|')]);

        SimpleDB.MigrateFile(
          core.PeopleM.DataAdapter.TableFilePath,
          record => $"{record}||");

        SimpleDB.MigrateFile(
          core.ViewersM.DataAdapter.TableFilePath,
          record => {
            var lio = record.LastIndexOf('|');
            return $"{record[..lio]}||{record[lio..]}";
          });

        oldVersion = newVersion;
      }

      // 1 => 2
      if (oldVersion == 1) {
        core.FavoriteFoldersM.DataAdapter.IsModified = true;
        core.FoldersM.DataAdapter.IsModified = true;
        core.MediaItemsM.DataAdapter.IsModified = true;
        core.SegmentsM.DataAdapter.IsModified = true;
        core.VideoClipsM.DataAdapter.IsModified = true;
        core.VideoClipsM.GroupsM.DataAdapter.IsModified = true;
      }
    }
  }
}
