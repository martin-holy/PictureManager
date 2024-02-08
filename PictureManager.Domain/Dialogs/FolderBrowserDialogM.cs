using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace PictureManager.Domain.Dialogs;

public sealed class FolderBrowserDialogM : Dialog {
  private FolderTreeViewItem _selectedFolder;

  public FolderTreeViewItem SelectedFolder { get => _selectedFolder; private set { _selectedFolder = value; OnPropertyChanged(); } }
  public ObservableCollection<FolderTreeViewItem> Drives { get; } = [];
  public RelayCommand<FolderTreeViewItem> SelectCommand { get; }

  public FolderBrowserDialogM() : base("Browse For Folder", Res.IconFolder) {
    SelectCommand = new(x => SelectedFolder = x);
    Buttons = new DialogButton[] {
      new(OkCommand, true),
      new(CloseCommand, false, true) };

    AddDrives();
  }

  private void AddDrives() {
    var drives = Environment.GetLogicalDrives();

    foreach (var drive in drives) {
      var di = new DriveInfo(drive);
      if (!di.IsReady) continue;

      var item = new FolderTreeViewItem(null, di.Name.TrimEnd(Path.DirectorySeparatorChar)) {
        Icon = FoldersM.GetDriveIcon(di.DriveType)
      };

      // add placeholder so the Drive can be expanded
      item.Items.Add(new FolderTreeViewItem(null, null));

      Drives.Add(item);
    }
  }
}

public class FolderTreeViewItem : TreeItem {
  public string FullPath => this.GetFullName(Path.DirectorySeparatorChar.ToString(), x => x.Name);

  public FolderTreeViewItem(ITreeItem parent, string name) : base(Res.IconFolder, name) {
    Parent = parent;
  }

  protected override void OnIsExpandedChanged(bool value) {
    if (value) LoadSubFolders();
    UpdateIcon();
  }

  private void UpdateIcon() {
    if (Parent != null) // not Drive Folder
      Icon = IsExpanded
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
        using var enumerator = Directory.EnumerateDirectories(folder.FullPath).GetEnumerator();
        if (enumerator.MoveNext())
          folder.Items.Add(new FolderTreeViewItem(null, null));

        // add new Folder to the tree if is Accessible
        Items.Add(folder);
      }
      catch (UnauthorizedAccessException) { }
    }
  }
}