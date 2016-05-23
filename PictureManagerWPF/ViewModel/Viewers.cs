using System;
using System.Linq;
using System.Windows;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class Viewers : BaseTreeViewItem {

    public Viewers() {
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

    public void NewOrRenameViewer(Viewer viewer, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = ACore.WMain,
        IconName = "appbar_eye",
        Title = rename ? "Rename Viewer" : "New Viewer",
        Question = rename ? "Enter the new name of the viewer." : "Enter the name of the new viewer.",
        Answer = rename ? viewer.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, viewer.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (ACore.Db.Viewers.SingleOrDefault(x => x.Name.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("Viewer's name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          viewer.Title = inputDialog.Answer;
          (viewer.Data).Name = inputDialog.Answer;
          ACore.Db.UpdateOnSubmit(viewer.Data);
          ACore.Db.SubmitChanges();
          SetInPalce(viewer, false);
        } else CreateViewer(inputDialog.Answer);
      }
    }

    public void SetInPalce(Viewer viewer, bool isNew) {
      var idx = ACore.Db.Viewers.OrderBy(x => x.Name).ToList().IndexOf(viewer.Data);
      if (isNew)
        Items.Insert(idx, viewer);
      else
        Items.Move(Items.IndexOf(viewer), idx);
    }

    public Viewer CreateViewer(string name) {
      var dmViewer = new DataModel.Viewer {
        Id = ACore.Db.GetNextIdFor<DataModel.Viewer>(),
        Name = name
      };

      ACore.Db.InsertOnSubmit(dmViewer);
      ACore.Db.SubmitChanges();

      var vmViewer = new Viewer(dmViewer);
      SetInPalce(vmViewer, true);
      return vmViewer;
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
