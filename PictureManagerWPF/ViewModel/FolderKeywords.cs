using System.IO;
using System.Linq;
using PictureManager.Database;

namespace PictureManager.ViewModel {
  public sealed class FolderKeywords: BaseCategoryItem {
    public FolderKeywords() : base (Category.FolderKeywords) {
      Title = "Folder Keywords";
      IconName = IconName.Folder;
    }

    public void Load() {
      Items.Clear();

      var fkRoots = ACore.Folders.All.Where(x => x.IsFolderKeyword);

      foreach (var fkRoot in fkRoots)
        LoadRecursive(fkRoot, null);
    }

    private void LoadRecursive(BaseTreeViewItem folder, BaseTreeViewItem folderKeyword) {
      foreach (var fi in folder.Items.Cast<Folder>()) {
        // TODO check jesli jsou nasledujici 2 podminky potreba az to bude vsechno hotovy
        if (!Directory.Exists(fi.FullPath)) continue;
        if (!ACore.CanViewerSeeThisDirectory(fi)) continue;

        var fk = (FolderKeyword) folderKeyword?.Items.SingleOrDefault(x => x.Title.Equals(fi.Title));
        if (fk == null) {
          fk = new FolderKeyword {
            Title = fi.Title,
            Parent = folderKeyword ?? this
          };
          (folderKeyword ?? this).Items.Add(fk);
        }

        fi.FolderKeyword = fk;
        fk.Folders.Add(fi);

        LoadRecursive(fi, fk);
      }
    }
  }
}
