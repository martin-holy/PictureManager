using System.Linq;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class Viewer : BaseTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }

    public BaseTreeViewItem IncludedFolders { get; }
    public BaseTreeViewItem ExcludedFolders { get; }

    public Viewer(int id, string name, BaseTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;

      IncludedFolders = new BaseTreeViewItem { Title = "Included Folders", IconName = IconName.FolderStar, Parent = this };
      ExcludedFolders = new BaseTreeViewItem { Title = "Excluded Folders", IconName = IconName.FolderStar, Parent = this };

      Items.Add(IncludedFolders);
      Items.Add(ExcludedFolders);

      IconName = IconName.Eye;
    }

    public string ToCsv() {
      // ID|Name|IncludedFolders|ExcludedFolders
      return string.Join("|",
        Id.ToString(),
        Title,
        string.Join(",", IncludedFolders.Items.Select(x => x.Tag)),
        string.Join(",", ExcludedFolders.Items.Select(x => x.Tag)));
    }

    public void AddFolder(bool included) {
      // TODO asi by bylo lepsi nez zobrazovat Windows only dialog, moznost vybrat slozku z Folders
      /*var dir = new FolderBrowserDialog();
      if (dir.ShowDialog() != DialogResult.OK) return;
      if ((included ? IncludedFolders : ExcludedFolders).Items.Any(x => x.ToolTip.Equals(dir.SelectedPath))) return;

      var dmViewerAccess = new DataModel.ViewerAccess {
        Id = ACore.Db.GetNextIdFor<DataModel.ViewerAccess>(),
        ViewerId = Data.Id,
        IsIncluded = included,
        DirectoryId = ACore.Db.InsertDirectoryInToDb(dir.SelectedPath)
      };

      ACore.Db.Insert(dmViewerAccess);
      SetInPlace(InitFolder(dmViewerAccess, included, dir.SelectedPath));*/
    }

    private static void SetInPlace(BaseTreeViewItem item) {
      item.Parent.Items.Add(item);
      var idx = item.Parent.Items.OrderBy(x => x.ToolTip).ToList().IndexOf(item);
      item.Parent.Items.Move(item.Parent.Items.IndexOf(item), idx);
    }
  }
}
