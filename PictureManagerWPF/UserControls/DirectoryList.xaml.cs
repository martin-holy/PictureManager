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

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ObservableCollection<Data.BaseItem> FoldersRoot;
    public Data.Folders Folders;

    private string _settingsPropertyName;
    public string SettingsPropertyName {
      get {
        return _settingsPropertyName;
      }
      set {
        _settingsPropertyName = value;
        LoadFolders();
        Folders.IsExpanded = true;
        Focus();
      }
    }

    public DirectoryList() {
      InitializeComponent();

      Folders = new Data.Folders { Title = "Folders", IconName = "appbar_folder" };
      FoldersRoot = new ObservableCollection<Data.BaseItem> { Folders };
      ItemsSource = FoldersRoot;
    }

    private List<string> GetPaths() {
      return ((string) Settings.Default[SettingsPropertyName]).Split(new[] {Environment.NewLine},
        StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x).ToList();
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseRightButtonDown on StackPanel in TreeView
      e.Handled = true;
      StackPanel stackPanel = (StackPanel) sender;
      object item = stackPanel.DataContext;

      if (stackPanel.ContextMenu != null) return;
      ContextMenu menu = new ContextMenu {Tag = item};

      switch (item.GetType().Name) {
        case nameof(Data.Folders): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderAdd"], CommandParameter = item});
          break;
        }
        case nameof(Data.Folder): {
          if (((Data.Folder) item).Parent == null) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRemove"], CommandParameter = item});
          }
          break;
        }
      }

      if (menu.Items.Count > 0)
        stackPanel.ContextMenu = menu;
    }

    private void LoadFolders() {
      var paths = GetPaths();
      Folders.Items.Clear();
      foreach (var path in paths) {
        DirectoryInfo di = new DirectoryInfo(path);
        Data.Folder item = new Data.Folder {
          Title = di.Name,
          FullPath = path,
          IconName = "appbar_folder",
          IsAccessible = true
        };
        try {
          if (di.GetDirectories().Length > 0)
            item.Items.Add(new Data.Folder { Title = "..." });
        } catch (UnauthorizedAccessException) {
          item.IconName = "appbar_folder_lock";
          item.IsAccessible = false;
        } catch (DirectoryNotFoundException) {
          item.IconName = "appbar_folder_lock";
          item.IsAccessible = false;
        } finally {
          Folders.Items.Add(item);
        }
      }
    }

    private void CmdFolderAdd(object sender, ExecutedRoutedEventArgs e) {
      FolderBrowserDialog dir = new FolderBrowserDialog();
      if (dir.ShowDialog() == DialogResult.OK) {
        var paths = GetPaths();
        paths.Add(dir.SelectedPath);
        Settings.Default[SettingsPropertyName] = string.Join(Environment.NewLine, paths);
        Settings.Default.Save();
        LoadFolders();
      }
    }

    private void CmdFolderRemove(object sender, ExecutedRoutedEventArgs e) {
      var folder = e.Parameter as Data.Folder;
      if (folder == null) return;
      var paths = GetPaths();
      paths.Remove(folder.FullPath);
      Settings.Default[SettingsPropertyName] = string.Join(Environment.NewLine, paths);
      Settings.Default.Save();
      LoadFolders();
    }
  }
}
