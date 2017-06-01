using System;
using System.Linq;
using System.Windows;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class Viewers : BaseCategoryItem {

    public Viewers() : base(Categories.Viewers) {
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
    }

    public Viewer CreateViewer(string name) {
      var dmViewer = new DataModel.Viewer {
        Id = ACore.Db.GetNextIdFor<DataModel.Viewer>(),
        Name = name
      };

      ACore.Db.Insert(dmViewer);

      var vmViewer = new Viewer(dmViewer);
      ACore.Viewers.ItemSetInPlace(this, true, vmViewer);
      return vmViewer;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      InputDialog inputDialog = ItemGetInputDialog(item, "appbar_eye", "Viewer", rename);

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          var viewer = (Viewer)item;
          viewer.Title = inputDialog.Answer;
          viewer.Data.Name = inputDialog.Answer;
          ACore.Db.Update(viewer.Data);
          ACore.Viewers.ItemSetInPlace(viewer.Parent, false, viewer);
        } else CreateViewer(inputDialog.Answer);
      }
    }

    public override void ItemDelete(BaseTreeViewTagItem item) {
      //TODO: SubmitChanges can submit other not commited changes as well!!
      var viewer = item as Viewer;
      if (viewer == null) return;

      foreach (var v in ACore.Db.ViewersAccess.Where(x => x.ViewerId == viewer.Id)) {
        ACore.Db.DeleteOnSubmit(v);
      }

      ACore.Db.DeleteOnSubmit(viewer.Data);
      ACore.Db.SubmitChanges();

      item.Parent.Items.Remove(viewer);
    }

    public void AddFolder(bool included, string path) {
      var editedViewer = (Viewer) Application.Current.Properties[nameof(AppProps.EditedViewer)];
      editedViewer?.AddFolder(included, path);
    }

    public void RemoveFolder(BaseTreeViewItem folder) {
      var data = folder.Tag as DataModel.ViewerAccess;
      if (data == null) return;

      var viewer = Items.Cast<Viewer>().SingleOrDefault(x => x.Id == data.ViewerId);
      if (viewer == null) return;

      ACore.Db.DeleteOnSubmit(data);
      (data.IsIncluded ? viewer.IncludedFolders : viewer.ExcludedFolders).Items.Remove(folder);
      ACore.Db.SubmitChanges();
    }
  }
}
