using System;
using System.IO;

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

namespace PictureManager.Domain.DataAdapters {
  public static class DatabaseMigration {
    public static void Resolver(int oldVersion, int newVersion) {
      var core = Core.Instance;
      if (oldVersion == 0 && newVersion == 1) {
        SimpleDB.SimpleDB.MigrateFile(
          core.FavoriteFoldersM.DataAdapter.TableFilePath,
          record => $"{record}|Favorite folder name");

         SimpleDB.SimpleDB.MigrateFile(
          core.KeywordsM.DataAdapter.TableFilePath,
          record => record[..record.LastIndexOf('|')]);

        SimpleDB.SimpleDB.MigrateFile(
          core.PeopleM.DataAdapter.TableFilePath,
          record => $"{record}||");

        SimpleDB.SimpleDB.MigrateFile(
          core.ViewersM.DataAdapter.TableFilePath,
          record => {
            var lio = record.LastIndexOf('|');
            return $"{record[..lio]}||{record[lio..]}";
          });
      }
    }
  }
}
