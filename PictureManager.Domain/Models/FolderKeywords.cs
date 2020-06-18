using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeywords: BaseCategoryItem {
    public FolderKeywords() : base (Category.FolderKeywords) {
      Title = "Folder Keywords";
      IconName = IconName.FolderPuzzle;
    }

    public void Load() {
      Items.Clear();

      var fkRoots = Core.Instance.Folders.All.Where(x => x.IsFolderKeyword);

      foreach (var fkRoot in fkRoots) {
        if (fkRoot.IsThisOrParentHidden()) continue;
        LoadRecursive(fkRoot, this);
      }
    }

    private static void LoadRecursive(BaseTreeViewItem folder, BaseTreeViewItem folderKeyword) {
      foreach (var fi in folder.Items.OfType<Folder>()) {
        if (!Core.Instance.CanViewerSeeThisFolder(fi)) continue;
        if (fi.IsThisOrParentHidden()) continue;

        // create new FolderKeyword if doesn't exists
        if (!(folderKeyword.Items.SingleOrDefault(x => x.Title.Equals(fi.Title)) is FolderKeyword fk)) {
          fk = new FolderKeyword {
            Title = fi.Title,
            Parent = folderKeyword
          };
          fk.Parent.Items.Add(fk);
        }

        fi.FolderKeyword = fk;
        fk.Folders.Add(fi);

        LoadRecursive(fi, fk);
      }

      folderKeyword.Items.Sort(x => x.Title);
    }
  }
}
