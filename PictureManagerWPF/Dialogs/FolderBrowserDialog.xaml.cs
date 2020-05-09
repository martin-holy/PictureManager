using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.Dialogs {
  public partial class FolderBrowserDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ObservableCollection<FolderTreeViewItem> Drives;
    public string SelectedPath => ((FolderTreeViewItem) TreeViewFolders.SelectedValue)?.FullPath;

    public FolderBrowserDialog(Window owner) {
      Owner = owner;
      InitializeComponent();

      Drives = new ObservableCollection<FolderTreeViewItem>();
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
        item.Items.Add(new BaseTreeViewItem());

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

  public class FolderTreeViewItem : BaseTreeViewItem {
    public string FullPath => GetFullPath(Path.DirectorySeparatorChar.ToString());
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
          Title = dir.Substring(fullPath.Length),
          Parent = this,
          IconName = IconName.Folder
        };

        try {
          // add placeholder so the folder can be expanded
          if (Directory.EnumerateDirectories(folder.FullPath).GetEnumerator().MoveNext())
            folder.Items.Add(new BaseTreeViewItem());

          // add new Folder to the tree if is Accessible
          Items.Add(folder);
        }
        catch (UnauthorizedAccessException) { }
      }
    }
  }
}
