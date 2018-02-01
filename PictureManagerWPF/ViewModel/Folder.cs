using System;
using System.IO;
using System.Linq;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class Folder : BaseTreeViewItem {
    private bool _isAccessible;
    public bool IsAccessible { get => _isAccessible; set { _isAccessible = value; OnPropertyChanged(); } }
    public string FullPath { get; set; }
    public override bool IsExpanded {
      get => base.IsExpanded;
      set {
        base.IsExpanded = value;
        if (value) GetSubFolders(false);
        if (Parent != null)
          IconName = IsExpanded ? "appbar_folder_open" : "appbar_folder";
      }
    }

    public Folder() {
      IconName = "appbar_folder";
    }

    public void Rename(string newName) {
      if (Parent.Items.Any(x => x.Title.Equals(newName))) return;
      if (!ACore.FileOperation(FileOperations.Move, FullPath, ((Folder) Parent).FullPath, newName)) return;
      UpdateFullPath(FullPath, Path.Combine(((Folder) Parent).FullPath, newName));
      Title = newName;
    }

    public void GetSubFolders(bool refresh) {
      if (!refresh) {
        if (Items.Count <= 0) return;
        if (Items[0].Title != @"...") return;
      }
      Items.Clear();

      foreach (var dir in Directory.GetDirectories(FullPath).OrderBy(x => x)) {
        if (!ACore.CanViewerSeeThisDirectory(dir)) continue;
        var di = new DirectoryInfo(dir);
        var item = new Folder {
          Title = di.Name,
          FullPath = dir,
          Parent = this,
          IsAccessible = true
        };
        try {
          if (di.GetDirectories().Length > 0)
            item.Items.Add(new Folder {Title = "..."});
        } catch (UnauthorizedAccessException) {
          item.IconName = "appbar_folder_lock";
          item.IsAccessible = false;
        } finally {
          Items.Add(item);
        }
      }
    }

    public void UpdateFullPath(string oldParentPath, string newParentPath) {
      oldParentPath = oldParentPath.EndsWith("\\") ? oldParentPath.Substring(0, oldParentPath.Length - 1) : oldParentPath;
      newParentPath = newParentPath.EndsWith("\\") ? newParentPath.Substring(0, newParentPath.Length - 1) : newParentPath;
      FullPath = FullPath?.Replace(oldParentPath, newParentPath);
      foreach (var item in Items.Cast<Folder>()) {
        item.UpdateFullPath(oldParentPath, newParentPath);
      }
    }

    public Folder New(string folderName) {
      IsExpanded = true;
      var newFullPath = $"{FullPath}\\{folderName}";
      Directory.CreateDirectory(newFullPath);
      ACore.Db.InsertDirecotryInToDb(newFullPath);

      var newFolder = new Folder {
        Title = folderName,
        FullPath = newFullPath,
        Parent = this,
        IsAccessible = true
      };

      var folder = Items.Cast<Folder>().FirstOrDefault(f => string.Compare(f.Title, folderName, StringComparison.OrdinalIgnoreCase) >= 0);
      Items.Insert(folder == null ? Items.Count : Items.IndexOf(folder), newFolder);

      return newFolder;
    }

    public void Delete(bool recycle) {
      if (!ACore.FileOperation(FileOperations.Delete, FullPath, recycle)) return;
      Parent.Items.Remove(this);
    }

    public bool IsItCorrectFolderName(string name) {
      string[] incorectChars = {"\\", "/", ":", "*", "?", "\"", "<", ">", "|"};
      return !incorectChars.Any(name.Contains);
    }

    public void NewOrRename(bool rename) {
      var inputDialog = new InputDialog {
        Owner = ACore.WMain,
        IconName = "appbar_folder",
        Title = rename ? "Rename Folder" : "New Folder",
        Question = rename ? "Enter the new name for the folder." : "Enter the name of the new folder.",
        Answer = rename ? Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (Directory.Exists($"{(rename ? ((Folder) Parent).FullPath : FullPath)}\\{inputDialog.TxtAnswer.Text}")) {
          inputDialog.ShowErrorMessage("Folder already exists!");
          return;
        }

        if (!IsItCorrectFolderName(inputDialog.TxtAnswer.Text)) {
          inputDialog.ShowErrorMessage("New folder's name contains incorrect character(s)!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) Rename(inputDialog.Answer);
        else New(inputDialog.Answer);
      }
    }
  }
}
