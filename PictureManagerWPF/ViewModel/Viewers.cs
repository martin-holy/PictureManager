using System;
using System.Linq;
using System.Windows;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class Viewers : BaseTreeViewItem {
    public DataModel.PmDataContext Db;

    public Viewers() {
      Title = "Viewers";
      IconName = "appbar_eye";
    }

    public void Load() {
      Items.Clear();
     
      foreach (var viewer in Db.Viewers.OrderBy(x => x.Name).Select(x => new Viewer(Db, x))) {
        viewer.Parent = this;
        Items.Add(viewer);
      }
      IsExpanded = true;
    }

    public void NewOrRenameViewer(WMain wMain, Viewer viewer, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = wMain,
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

        if (Db.Viewers.SingleOrDefault(x => x.Name.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("Viewer's name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          viewer.Title = inputDialog.Answer;
          ((DataModel.Viewer)viewer.DbData).Name = inputDialog.Answer;
          Db.UpdateOnSubmit((DataModel.Viewer)viewer.DbData);
          Db.SubmitChanges();
          SetInPalce(viewer, false);
        } else CreateViewer(inputDialog.Answer);
      }
    }

    public void SetInPalce(Viewer viewer, bool isNew) {
      var idx = Db.Viewers.OrderBy(x => x.Name).ToList().IndexOf((DataModel.Viewer)viewer.DbData);
      if (isNew)
        Items.Insert(idx, viewer);
      else
        Items.Move(Items.IndexOf(viewer), idx);
    }

    public Viewer CreateViewer(string name) {
      var dmViewer = new DataModel.Viewer {
        Id = Db.GetNextIdFor("Viewers"),
        Name = name
      };

      Db.InsertOnSubmit(dmViewer);
      Db.SubmitChanges();

      var vmViewer = new Viewer(Db, dmViewer);
      SetInPalce(vmViewer, true);
      return vmViewer;
    }

    public void AddFolder(bool included, string path) {
      var editedViewer = (Viewer) Application.Current.Properties[nameof(AppProps.EditedViewer)];
      editedViewer?.AddFolder(included, path);
    }

    public void RemoveFolder(BaseTreeViewItem folder) {
      var data = folder.DbData as DataModel.ViewerAccess;
      if (data == null) return;

      var viewer = Items.Cast<Viewer>().SingleOrDefault(x => x.Id == data.ViewerId);
      if (viewer == null) return;

      Db.DeleteOnSubmit(data);
      (data.IsIncluded ? viewer.IncludedFolders : viewer.ExcludedFolders).Items.Remove(folder);
      Db.SubmitChanges();
    }
  }
}
