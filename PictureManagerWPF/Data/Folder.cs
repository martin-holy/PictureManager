using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

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

    public void New() {
      IsExpanded = true;
      const string newFolderName = "New Folder";

      int i = 0;
      while (Items.Any(x => x.Title.Equals($"{newFolderName} {i}"))) {
        i++;
      }

      var newFullPath = $"{FullPath}\\{newFolderName} {i}";
      Directory.CreateDirectory(newFullPath);

      var newFolder = new Folder {
        Title = $"{newFolderName} {i}",
        FullPath = newFullPath,
        IconName = "appbar_folder",
        Parent = this,
        IsAccessible = true
      };

      Items.Insert(0, newFolder);
    }

    public void Delete(AppCore aCore, bool recycle) {
      if (!aCore.FileOperation(AppCore.FileOperations.Delete, FullPath, recycle)) return;
      Parent.Items.Remove(this);
    }
  }
}
