using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.IO;

namespace PictureManager.Common.Features.Folder;

public sealed class FolderBrowserDialog : Dialog {
  private FolderTreeViewItem? _selectedFolder;

  public FolderTreeViewItem? SelectedFolder { get => _selectedFolder; private set { _selectedFolder = value; OnPropertyChanged(); } }
  public TreeView TreeView { get; } = new() { ShowTreeItemSelection = true };

  public new RelayCommand OkCommand { get; }

  public FolderBrowserDialog() : base("Browse For Folder", Res.IconFolder) {
    TreeView.ItemSelectedEvent += (_, item) => SelectedFolder = item as FolderTreeViewItem;
    OkCommand = new(() => SetResult(this, 1), () => SelectedFolder != null, null, "Ok");
    Buttons = [
      new(OkCommand, true),
      new(CloseCommand, false, true)
    ];

    AddDrives();
  }

  private void AddDrives() {
    var drives = Environment.GetLogicalDrives();
    TreeView.RootHolder.Clear();
    TreeView.SelectedTreeItems.DeselectAll();

    foreach (var drive in drives) {
      var di = new DriveInfo(drive);
      if (!di.IsReady) continue;

      var item = new FolderTreeViewItem(null, di.Name.TrimEnd(Path.DirectorySeparatorChar)) {
        Icon = FolderS.GetDriveIcon(di.DriveType)
      };

      // add placeholder so the Drive can be expanded
      item.Items.Add(FolderTreeViewItem.Dummy);

      TreeView.RootHolder.Add(item);
    }
  }
}

public class FolderTreeViewItem : TreeItem {
  public static FolderTreeViewItem Dummy { get; } = new(null, string.Empty);
  public string FullPath => this.GetFullName(Path.DirectorySeparatorChar.ToString(), x => x.Name);

  public FolderTreeViewItem(ITreeItem? parent, string name) : base(Res.IconFolder, name) {
    Parent = parent;
  }

  protected override void _onIsExpandedChanged(bool value) {
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
          folder.Items.Add(Dummy);

        // add new Folder to the tree if is Accessible
        Items.Add(folder);
      }
      catch (UnauthorizedAccessException) { }
    }
  }
}