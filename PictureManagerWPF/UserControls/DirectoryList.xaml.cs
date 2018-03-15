using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using PictureManager.Properties;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace PictureManager.UserControls {
  /// <summary>
  /// Interaction logic for DirectoryList.xaml
  /// </summary>
  public partial class DirectoryList : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ObservableCollection<ViewModel.BaseTreeViewItem> FoldersRoot;
    public ViewModel.Folders Folders;
    public List<string> Paths; 

    private string _settingsPropertyName;
    public string SettingsPropertyName {
      get => _settingsPropertyName;
      set {
        _settingsPropertyName = value;
        LoadPathsFromSettings();
        LoadFolders();
        Folders.IsExpanded = true;
        Focus();
      }
    }

    public DirectoryList() {
      InitializeComponent();

      Paths = new List<string>();
      Folders = new ViewModel.Folders { Title = "Folders", IconName = IconName.Folder };
      FoldersRoot = new ObservableCollection<ViewModel.BaseTreeViewItem> { Folders };
      ItemsSource = FoldersRoot;
    }

    private void LoadPathsFromSettings() {
      Paths.Clear();
      Paths.AddRange(((string) Settings.Default[_settingsPropertyName]).Split(new[] {Environment.NewLine},
        StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x).ToList());
    }

    public void LoadPathsFromList(List<string> paths) {
      Paths.Clear();
      if (paths != null)
        Paths.AddRange(paths);
      LoadFolders();
      Folders.IsExpanded = true;
      Focus();
    }

    public void SavePathsToSettings() {
      Settings.Default[_settingsPropertyName] = string.Join(Environment.NewLine, Paths);
      Settings.Default.Save();
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseRightButtonDown on StackPanel in TreeView
      e.Handled = true;
      var stackPanel = (StackPanel) sender;
      var item = stackPanel.DataContext;

      if (stackPanel.ContextMenu != null) return;
      var menu = new ContextMenu {Tag = item};

      switch (item.GetType().Name) {
        case nameof(ViewModel.Folders): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderAdd"], CommandParameter = item});
          break;
        }
        case nameof(ViewModel.Folder): {
          if (((ViewModel.Folder) item).Parent == null) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRemove"], CommandParameter = item});
          }
          break;
        }
      }

      if (menu.Items.Count > 0)
        stackPanel.ContextMenu = menu;
    }

    private void LoadFolders() {
      Folders.Items.Clear();
      foreach (var path in Paths.OrderBy(x => x)) {
        var di = new DirectoryInfo(path);
        var item = new ViewModel.Folder {
          Title = di.Name,
          FullPath = path,
          IconName = IconName.Folder,
          IsAccessible = true
        };
        try {
          if (di.GetDirectories().Length > 0)
            item.Items.Add(new ViewModel.Folder { Title = "..." });
        } catch (UnauthorizedAccessException) {
          item.IconName = IconName.FolderLock;
          item.IsAccessible = false;
        } catch (DirectoryNotFoundException) {
          item.IconName = IconName.FolderLock;
          item.IsAccessible = false;
        } finally {
          Folders.Items.Add(item);
        }
      }
    }

    private void CmdFolderAdd(object sender, ExecutedRoutedEventArgs e) {
      var dir = new FolderBrowserDialog();
      if (dir.ShowDialog() != DialogResult.OK) return;
      var path = $"{dir.SelectedPath}{(dir.SelectedPath.EndsWith("\\") ? string.Empty : "\\")}";
      if (Paths.Contains(path)) return;
      Paths.Add(path);
      LoadFolders();
    }

    private void CmdFolderRemove(object sender, ExecutedRoutedEventArgs e) {
      var folder = e.Parameter as ViewModel.Folder;
      if (folder == null) return;
      Paths.Remove(folder.FullPath);
      LoadFolders();
    }
  }
}
