using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

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

  public class FolderTreeViewItem : CatTreeViewItem {
    private string _iconName;

    public string Title { get; }
    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string FullPath => Tree.GetFullName(this, Path.DirectorySeparatorChar.ToString(), x => x.Title);

    public FolderTreeViewItem(ITreeBranch parent, string title) {
      Parent = parent;
      Title = title;

      OnExpandedChanged += (_, _) => {
        if (IsExpanded)
          LoadSubFolders();
        UpdateIconName();
      };
    }

    private void UpdateIconName() {
      if (Parent != null) // not Drive Folder
        IconName = IsExpanded ? "IconFolderOpen" : "IconFolder";
    }

    public void LoadSubFolders() {
      // remove placeholder
      if (Items.Count == 1 && ((ICatTreeViewItem)Items[0]).Parent == null) Items.Clear();

      var fullPath = FullPath + Path.DirectorySeparatorChar;

      foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
        var folder = new FolderTreeViewItem(this, dir[fullPath.Length..]) { IconName = "IconFolder" };

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
