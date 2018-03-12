using System.Linq;
using System.Windows;

namespace PictureManager.ViewModel {
  public sealed class Viewers : BaseCategoryItem {

    public Viewers() : base(Category.Viewers) {
      Title = "Viewers";
      IconName = "appbar_eye";
     }

    public void Load() {
      Items.Clear();
     
      foreach (var viewer in ACore.Db.Viewers.OrderBy(x => x.Name).Select(x => new Viewer(x))) {
        viewer.Parent = this;
        Items.Add(viewer);
      }
      IsExpanded = true;

      if (Items.Count == 0) AppCore.WMain.MenuViewers.Visibility = Visibility.Collapsed;
    }

    public void CreateViewer(string name) {
      var dmViewer = new DataModel.Viewer {
        Id = ACore.Db.GetNextIdFor<DataModel.Viewer>(),
        Name = name
      };

      ACore.Db.Insert(dmViewer);

      var vmViewer = new Viewer(dmViewer);
      ACore.Viewers.ItemSetInPlace(this, true, vmViewer);
      AppCore.WMain.MenuViewers.Visibility = Visibility.Visible;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      var inputDialog = ItemGetInputDialog(item, "appbar_eye", "Viewer", rename);

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) {
        var viewer = (Viewer) item;
        viewer.Title = inputDialog.Answer;
        ACore.Db.Update(viewer.Data);
        ACore.Viewers.ItemSetInPlace(viewer.Parent, false, viewer);
      } else CreateViewer(inputDialog.Answer);
    }

    public override void ItemDelete(BaseTreeViewItem item) {
      if (!(item is Viewer viewer)) return;
      var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();

      foreach (var v in ACore.Db.ViewersAccess.Where(x => x.ViewerId == viewer.Data.Id)) {
        DataModel.PmDataContext.DeleteOnSubmit(v, lists);
      }

      DataModel.PmDataContext.DeleteOnSubmit(viewer.Data, lists);
      ACore.Db.SubmitChanges(lists);

      item.Parent.Items.Remove(viewer);
      if (Items.Count == 0) AppCore.WMain.MenuViewers.Visibility = Visibility.Collapsed;
    }

    public void RemoveFolder(BaseTreeViewItem folder) {
      if (!(folder.Tag is DataModel.ViewerAccess data)) return;

      var viewer = Items.Cast<Viewer>().SingleOrDefault(x => x.Data.Id == data.ViewerId);
      if (viewer == null) return;

      ACore.Db.Delete(data);
      (data.IsIncluded ? viewer.IncludedFolders : viewer.ExcludedFolders).Items.Remove(folder);
    }
  }
}
