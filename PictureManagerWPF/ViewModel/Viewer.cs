using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PictureManager.ViewModel {
  public class Viewer : BaseTreeViewItem, IDbItem {
    public DataModel.Viewer Data;
    public override string Title { get => Data.Name; set { Data.Name = value; OnPropertyChanged(); } }
    public BaseTreeViewItem IncludedFolders;
    public BaseTreeViewItem ExcludedFolders;

    public Viewer(DataModel.Viewer data) {
      IncludedFolders = new BaseTreeViewItem { Title = "Included Folders", IconName = "appbar_folder_star", Parent = this };
      ExcludedFolders = new BaseTreeViewItem { Title = "Excluded Folders", IconName = "appbar_folder_star", Parent = this };

      Items.Add(IncludedFolders);
      Items.Add(ExcludedFolders);

      IconName = "appbar_eye";
      Data = data;

      ReLoad();
    }

    public void ReLoad() {
      LoadFolders(true);
      LoadFolders(false);
    }

    private void LoadFolders(bool included) {
      (included ? IncludedFolders : ExcludedFolders).Items.Clear();
      
      var dirs =
        from va in ACore.Db.ViewersAccess.Where(x => x.IsIncluded == included && x.ViewerId == Data.Id && x.DirectoryId != null)
        join d in ACore.Db.Directories on va.DirectoryId equals d.Id
        orderby d.Path
        select new KeyValuePair<DataModel.ViewerAccess, DataModel.Directory>(va, d);

      foreach (var dir in dirs) {
        var folder = InitFolder(dir.Key, included, dir.Value.Path);
        (included ? IncludedFolders : ExcludedFolders).Items.Add(folder);
      }
    }

    private BaseTreeViewItem InitFolder(DataModel.ViewerAccess data, bool included, string path) {
      return new BaseTreeViewItem {
        Tag = data,
        IconName = "appbar_folder",
        Title = GetTitleFromPath(path),
        ToolTip = path,
        Parent = included ? IncludedFolders : ExcludedFolders,
      };
    }

    public void AddFolder(bool included) {
      var dir = new FolderBrowserDialog();
      if (dir.ShowDialog() != DialogResult.OK) return;
      if ((included ? IncludedFolders : ExcludedFolders).Items.Any(x => x.ToolTip.Equals(dir.SelectedPath))) return;

      var dmViewerAccess = new DataModel.ViewerAccess {
        Id = ACore.Db.GetNextIdFor<DataModel.ViewerAccess>(),
        ViewerId = Data.Id,
        IsIncluded = included,
        DirectoryId = ACore.Db.InsertDirectoryInToDb(dir.SelectedPath)
      };

      ACore.Db.Insert(dmViewerAccess);
      SetInPlace(InitFolder(dmViewerAccess, included, dir.SelectedPath));
    }

    private static void SetInPlace(BaseTreeViewItem item) {
      item.Parent.Items.Add(item);
      var idx = item.Parent.Items.OrderBy(x => x.ToolTip).ToList().IndexOf(item);
      item.Parent.Items.Move(item.Parent.Items.IndexOf(item), idx);
    }

    private static string GetTitleFromPath(string path) {
      path = path.EndsWith("\\") ? path.Substring(0, path.Length - 1) : path;
      var lio = path.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
      return path.Substring(lio + 1, path.Length - lio - 1);
    }
  }
}
