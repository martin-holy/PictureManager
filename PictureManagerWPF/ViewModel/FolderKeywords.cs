using System.Collections.Generic;
using System.IO;
using System.Linq;
using PictureManager.Database;

namespace PictureManager.ViewModel {
  public sealed class FolderKeywords: BaseCategoryItem {
    public List<FolderKeyword> AllFolderKeywords = new List<FolderKeyword>();

    public FolderKeywords() : base (Category.FolderKeywords) {
      Title = "Folder Keywords";
      IconName = IconName.Folder;
    }

    public void Load() {
      Items.Clear();
      AllFolderKeywords.Clear();

      var fkRoots = ACore.Folders.Records.Values.Cast<Folder>().Where(x => x.IsFolderKeyword);

      foreach (var fkRoot in fkRoots) {
        var folderKeywordRoot = new FolderKeyword {Title = fkRoot.Title};
        Items.Add(folderKeywordRoot);
        AllFolderKeywords.Add(folderKeywordRoot);
        LoadRecursive(fkRoot, folderKeywordRoot);
      }
    }

    private void LoadRecursive(Folder folder, FolderKeyword folderKeyword) {
      foreach (var folderItem in folder.Items) {
        var fi = (Folder) folderItem;
        if (!Directory.Exists(fi.FullPath)) continue;
        if (!ACore.CanViewerSeeThisDirectory(fi.FullPath)) continue;

        var fk = (FolderKeyword) folderKeyword.Items.SingleOrDefault(x => x.Title.Equals(fi.Title));
        if (fk == null) {
          fk = new FolderKeyword {Title = fi.Title, Parent = folderKeyword};
          folderKeyword.Items.Add(fk);
          AllFolderKeywords.Add(fk);
        }

        fi.FolderKeyword = fk;
        fk.Folders.Add(fi);

        LoadRecursive(fi, fk);
      }
    }
  }
}
