using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.Dialogs {
  public partial class FolderBrowserDialog {
    public ObservableCollection<FolderTreeViewItem> Drives { get; } = new();
    public string SelectedPath => ((FolderTreeViewItem)TreeViewFolders.SelectedValue)?.FullPath;

    public FolderBrowserDialog() {
      InitializeComponent();
      Owner = Application.Current.MainWindow;
      TreeViewFolders.ItemsSource = Drives;
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

    private void BtnOk_OnClick(object sender, RoutedEventArgs e) {
      DialogResult = true;
      Close();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      DialogResult = false;
      Close();
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
