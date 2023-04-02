using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.IO;
using System;
using PictureManager.Domain.Models;
using System.Collections.ObjectModel;

namespace PictureManager.Domain.Dialogs {
  public sealed class FolderBrowserDialogM : ObservableObject, IDialog {
    private string _title = "Browse For Folder";
    private int _result = -1;
    private FolderTreeViewItem _selectedFolder;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public FolderTreeViewItem SelectedFolder { get => _selectedFolder; set { _selectedFolder = value; OnPropertyChanged(); } }

    public ObservableCollection<FolderTreeViewItem> Drives { get; } = new();
    public RelayCommand<FolderTreeViewItem> SelectCommand { get; }
    public RelayCommand<object> OkCommand { get; }
    public RelayCommand<object> CancelCommand { get; }

    public FolderBrowserDialogM() {
      SelectCommand = new(x => SelectedFolder = x);
      OkCommand = new(() => Result = 1);
      CancelCommand = new(() => Result = 0);

      AddDrives();
    }

    private void AddDrives() {
      var drives = Environment.GetLogicalDrives();

      foreach (var drive in drives) {
        var di = new DriveInfo(drive);
        if (!di.IsReady) continue;

        var item = new FolderTreeViewItem(null, di.Name.TrimEnd(Path.DirectorySeparatorChar)) {
          IconName = FoldersM.GetDriveIconName(di.DriveType)
        };

        // add placeholder so the Drive can be expanded
        item.Items.Add(new FolderTreeViewItem(null, null));

        Drives.Add(item);
      }
    }
  }

  public class FolderTreeViewItem : TreeItem {
    public string FullPath => Tree.GetFullName(this, Path.DirectorySeparatorChar.ToString(), x => x.Name);

    public FolderTreeViewItem(ITreeItem parent, string name) : base(Res.IconFolder, name) {
      Parent = parent;

      ExpandedChangedEventHandler += (_, _) => {
        if (IsExpanded)
          LoadSubFolders();
        UpdateIconName();
      };
    }

    private void UpdateIconName() {
      if (Parent != null) // not Drive Folder
        IconName = IsExpanded
          ? Res.IconFolderOpen
          : Res.IconFolder;
    }

    private void LoadSubFolders() {
      var fullPath = FullPath + Path.DirectorySeparatorChar;
      Items.Clear();

      foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
        var folder = new FolderTreeViewItem(this, dir[fullPath.Length..]);

        try {
          // add placeholder so the folder can be expanded
          if (Directory.EnumerateDirectories(folder.FullPath).GetEnumerator().MoveNext())
            folder.Items.Add(new FolderTreeViewItem(null, null));

          // add new Folder to the tree if is Accessible
          Items.Add(folder);
        }
        catch (UnauthorizedAccessException) { }
      }
    }
  }
}
