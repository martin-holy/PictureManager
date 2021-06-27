using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager.Dialogs {
  public partial class FolderBrowserDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public ObservableCollection<FolderTreeViewItem> Drives { get; } = new();
    public string SelectedPath => ((FolderTreeViewItem)TreeViewFolders.SelectedValue)?.FullPath;

    public FolderBrowserDialog(Window owner) {
      InitializeComponent();
      Owner = owner;
      TreeViewFolders.ItemsSource = Drives;
      AddDrives();
    }

    private void AddDrives() {
      var drives = Environment.GetLogicalDrives();

      foreach (var drive in drives) {
        var di = new DriveInfo(drive);
        if (!di.IsReady) continue;

        var item = new FolderTreeViewItem {
          Title = di.Name.TrimEnd(Path.DirectorySeparatorChar),
          IconName = Folders.GetDriveIconName(di.DriveType)
        };

        // add placeholder so the Drive can be expanded
        item.Items.Add(new CatTreeViewItem());

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

  public class FolderTreeViewItem : CatTreeViewItem {
    public string FullPath => CatTreeViewUtils.GetFullPath(this, Path.DirectorySeparatorChar.ToString());
    public override bool IsExpanded {
      get => base.IsExpanded;
      set {
        base.IsExpanded = value;
        if (value) LoadSubFolders();
        if (Parent != null) // not Drive Folder
          IconName = IsExpanded ? IconName.FolderOpen : IconName.Folder;
      }
    }

    public void LoadSubFolders() {
      // remove placeholder
      if (Items.Count == 1 && Items[0].Title == null) Items.Clear();

      var fullPath = FullPath + Path.DirectorySeparatorChar;

      foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
        var folder = new FolderTreeViewItem {
          Title = dir[fullPath.Length..],
          Parent = this,
          IconName = IconName.Folder
        };

        try {
          // add placeholder so the folder can be expanded
          if (Directory.EnumerateDirectories(folder.FullPath).GetEnumerator().MoveNext())
            folder.Items.Add(new CatTreeViewItem());

          // add new Folder to the tree if is Accessible
          Items.Add(folder);
        }
        catch (UnauthorizedAccessException) { }
      }
    }
  }
}
