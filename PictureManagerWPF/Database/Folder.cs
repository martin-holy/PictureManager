using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using PictureManager.ViewModel;
using PictureManager.Dialogs;
using PictureManager.Properties;

namespace PictureManager.Database {
  public sealed class Folder : BaseTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }
    public bool IsFolderKeyword { get; set; }
    public List<BaseMediaItem> MediaItems { get; set; } = new List<BaseMediaItem>();
    public FolderKeyword FolderKeyword { get; set; }

    private bool _isAccessible;
    public bool IsAccessible { get => _isAccessible; set { _isAccessible = value; OnPropertyChanged(); } }
    public string FullPath => GetFullPath();
    public string FullPathCache => FullPath.Replace(":\\", Settings.Default.CachePath);
    public override bool IsExpanded {
      get => base.IsExpanded;
      set {
        base.IsExpanded = value;
        if (value) LoadSubFolders(false);
        if (Parent != null) // not Drive Folder
          IconName = IsExpanded ? IconName.FolderOpen : IconName.Folder;
      }
    }

    public Folder(int id, string name, BaseTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;
      IconName = IconName.Folder;
    }

    public string ToCsv() {
      // ID|Name|Parent|IsFolderKeyword
      return string.Join("|",
        Id.ToString(),
        Title,
        (Parent as Folder)?.Id.ToString(),
        IsFolderKeyword ? "1" : string.Empty);
    }

    public string GetFullPath() {
      var parent = Parent;
      var names = new List<string> {Title};
      while (parent != null) {
        names.Add(parent.Title);
        parent = parent.Parent as Folder;
      }

      names.Reverse();
      names.Add(string.Empty); // for the DirectorySeparatorChar on the end as well

      return string.Join(Path.DirectorySeparatorChar.ToString(), names);
    }

    public void Rename(string newName) {
      if (!ACore.FileOperation(FileOperationMode.Move, FullPath, ((Folder)Parent).FullPath, newName)) return;
      Title = newName;
      ACore.Folders.Helper.IsModifed = true;
    }

    public void LoadSubFolders(bool recursive) {
      // remove placeholder
      if (Items.Count == 1 && Items[0].Title.Equals("..."))
        Items.RemoveAt(0);

      var dirNames = new List<string>();
      foreach (var dir in Directory.EnumerateDirectories(FullPath)) {
        var isNew = false;
        var di = new DirectoryInfo(dir);
        dirNames.Add(di.Name);

        // get existing Folder in the tree
        var folder = (Folder) Items.SingleOrDefault(x => x.Title.Equals(di.Name));

        if (folder == null) {
          isNew = true;
          // add new Folder to the database
          folder = new Folder(ACore.Folders.Helper.GetNextId(), di.Name, this);
          ACore.Folders.AddRecord(folder);
        }

        // if Viewer can't see this Folder => remove Folder from the tree and continue
        if (!ACore.CanViewerSeeThisDirectory(folder)) {
          if (!isNew) Items.Remove(folder);
          continue;
        }

        // add new Folder to the tree
        if (isNew) Items.Add(folder);
      }

      // remove Folders deleted outside of this application
      foreach (var item in Items) {
        if (dirNames.Any(x => x.Equals(item.Title))) continue;
        ACore.Folders.DeleteRecord((Folder) item);
      }

      // add placeholder so the folder can be expanded
      // or if recursive => keep loading
      foreach (var item in Items.Cast<Folder>()) {
        // folder already have some items
        if (!recursive && item.Items.Count > 0) {
          item.IsAccessible = true;
          continue;
        }

        try {
          if (recursive) {
            item.LoadSubFolders(true);
          }
          else {
            if (Directory.EnumerateDirectories(item.FullPath).FirstOrDefault() != null) {
              item.Items.Add(new BaseTreeViewItem {Title = "..."});
            }
            item.IsAccessible = true;
          } 
        }
        catch (UnauthorizedAccessException) {
          item.IconName = IconName.FolderLock;
          item.IsAccessible = false;
        }
      }
      
      // sort Items
      Items.Sort(x => x.Title);
    }

    public Folder New(string folderName) {
      IsExpanded = true;
      try {
        // create Folder
        Directory.CreateDirectory(Path.Combine(FullPath, folderName));
        var item = new Folder(ACore.Folders.Helper.GetNextId(), folderName, this) { IsAccessible = true };

        // add new Folder to the database
        ACore.Folders.AddRecord(item);

        // add new Folder to the tree
        var folder = Items.FirstOrDefault(f => string.Compare(f.Title, folderName, StringComparison.OrdinalIgnoreCase) >= 0);
        Items.Insert(folder == null ? Items.Count : Items.IndexOf(folder), item);

        return item;
      }
      catch (Exception ex) {
        AppCore.ShowErrorDialog(ex);
        return null;
      }
    }

    public void NewOrRename(bool rename) {
      var inputDialog = new InputDialog {
        Owner = AppCore.WMain,
        IconName = IconName.Folder,
        Title = rename ? "Rename Folder" : "New Folder",
        Question = rename ? "Enter the new name for the folder." : "Enter the name of the new folder.",
        Answer = rename ? Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        // check if folder already exists
        if (Directory.Exists(Path.Combine(rename ? ((Folder)Parent).FullPath : FullPath, inputDialog.TxtAnswer.Text))) {
          inputDialog.ShowErrorMessage("Folder already exists!");
          return;
        }

        // check if is correct folder name
        if (Path.GetInvalidPathChars().Any(inputDialog.TxtAnswer.Text.Contains)) {
          inputDialog.ShowErrorMessage("New folder's name contains incorrect character(s)!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) Rename(inputDialog.Answer);
      else New(inputDialog.Answer);
    }

    public List<BaseMediaItem> GetMediaItemsRecursive() {
      // get all Folders
      var folders = new List<BaseTreeViewItem>();
      GetThisAndItemsRecursive(ref folders);

      // get all MediaItems from folders
      var mis = new List<BaseMediaItem>();
      foreach (var f in folders.Cast<Folder>())
        mis.AddRange(f.MediaItems);

      return mis;
    }
  }
}
