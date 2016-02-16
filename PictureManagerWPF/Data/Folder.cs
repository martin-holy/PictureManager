using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using PictureManager.Dialogs;

namespace PictureManager.Data {
  public class Folder: BaseItem {
    private bool _isAccessible;

    public override bool IsExpanded {
      get { return base.IsExpanded; }
      set {
        base.IsExpanded = value;
        if (value) GetSubFolders(false);
        if (Parent != null)
          IconName = IsExpanded ? "appbar_folder_open" : "appbar_folder";
      }
    }

    public bool IsAccessible { get { return _isAccessible; } set { _isAccessible = value; OnPropertyChanged("IsAccessible"); } }
    public string FullPath { get; set; }
    public ObservableCollection<Folder> Items { get; set; }
    public Folder Parent;

    public Folder() {
      Items = new ObservableCollection<Folder>();
    }

    public void Rename(AppCore aCore, string newName) {
      if (Parent.Items.Any(x => x.Title.Equals(newName))) return;
      if (!aCore.FileOperation(AppCore.FileOperations.Move, FullPath, Parent.FullPath, newName)) return;
      UpdateFullPath(FullPath, Path.Combine(Parent.FullPath, newName));
      Title = newName;
    }

    public void GetSubFolders(bool refresh) {
      if (!refresh) {
        if (Items.Count <= 0) return;
        if (Items[0].Title != @"...") return;
      }
      Items.Clear();

      foreach (string dir in Directory.GetDirectories(FullPath).OrderBy(x => x)) {
        DirectoryInfo di = new DirectoryInfo(dir);
        Folder item = new Folder() {
          Title = di.Name,
          FullPath = dir,
          IconName = "appbar_folder",
          Parent = this,
          IsAccessible = true
        };
        try {
          if (di.GetDirectories().Length > 0)
            item.Items.Add(new Folder() {Title = "..."});
        } catch (UnauthorizedAccessException) {
          item.IconName = "appbar_folder_lock";
          item.IsAccessible = false;
        } finally {
          Items.Add(item);
        }
      }
    }

    public void UpdateFullPath(string oldParentPath, string newParentPath) {
      FullPath = FullPath?.Replace(oldParentPath, newParentPath);
      foreach (var item in Items) {
        item.UpdateFullPath(oldParentPath, newParentPath);
      }
    }

    public Folder New(string folderName) {
      IsExpanded = true;
      var newFullPath = $"{FullPath}\\{folderName}";
      Directory.CreateDirectory(newFullPath);

      var newFolder = new Folder {
        Title = folderName,
        FullPath = newFullPath,
        IconName = "appbar_folder",
        Parent = this,
        IsAccessible = true
      };

      Folder folder = Items.FirstOrDefault(f => string.Compare(f.Title, folderName, StringComparison.OrdinalIgnoreCase) >= 0);
      Items.Insert(folder == null ? 0 : Items.IndexOf(folder), newFolder);

      return newFolder;
    }

    public void Delete(AppCore aCore, bool recycle) {
      if (!aCore.FileOperation(AppCore.FileOperations.Delete, FullPath, recycle)) return;
      Parent.Items.Remove(this);
    }

    public bool IsItCorrectFolderName(string name) {
      string[] incorectChars = {"\\", "/", ":", "*", "?", "\"", "<", ">", "|"};
      return !incorectChars.Any(name.Contains);
    }

    public void NewOrRename(WMain wMain, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = wMain,
        IconName = "appbar_folder",
        Title = rename ? "Rename Folder" : "New Folder",
        Question = rename ? "Enter new name for folder." : "Enter name of new folder.",
        Answer = rename ? Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (Directory.Exists($"{Parent.FullPath}\\{inputDialog.TxtAnswer.Text}")) {
          inputDialog.ShowErrorMessage("Folder already exists!");
          return;
        }

        if (!IsItCorrectFolderName(inputDialog.TxtAnswer.Text)) {
          inputDialog.ShowErrorMessage("New folder's name contains incorrect character(s)!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) Rename(wMain.ACore, inputDialog.Answer);
        else New(inputDialog.Answer);
      }
    }
  }
}
