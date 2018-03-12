using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using PictureManager.Properties;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WCatalog.xaml
  /// </summary>
  public partial class WCatalog: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ObservableCollection<ViewModel.BaseTreeViewItem> FoldersRoot;
    public ViewModel.Folders Folders;

    private AppCore ACore => (AppCore) System.Windows.Application.Current.Properties[nameof(AppProperty.AppCore)];
    private BackgroundWorker _update;
    private bool _justFilesCount;
    private int _filesCount;
    private int _filesDone;
    private string _selectedFolderPath;
    private bool _newOnly;
    private bool _rebuildThumbnails;
    private readonly List<DataModel.BaseTable>[] _lists;

    private string _filesProgress;
    public string FilesProgress { get => _filesProgress; set { _filesProgress = value; OnPropertyChanged(); } }

    public WCatalog() {
      InitializeComponent();

      _lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();
      Folders = new ViewModel.Folders { Title = "Folders", IconName = "appbar_folder" };
      FoldersRoot = new ObservableCollection<ViewModel.BaseTreeViewItem> { Folders };
      LoadFolders();
      Folders.IsExpanded = true;
      TvFolders.Focus();
      TvFolders.ItemsSource = FoldersRoot;
    }

    private void BtnUpdate_OnClick(object sender, RoutedEventArgs e) {
      _newOnly = ChbNewOnly.IsChecked == true;
      _rebuildThumbnails = ChbRebuildThumbs.IsChecked == true;

      var folder = TvFolders.SelectedItem as ViewModel.Folder;
      _selectedFolderPath = folder == null ? string.Empty : folder.FullPath;

      if (folder != null && !folder.IsAccessible) return;

      BtnUpdate.IsEnabled = false;
      _update = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
      _update.DoWork += update_DoWork;
      _update.ProgressChanged += Update_ProgressChanged;
      _update.RunWorkerCompleted += Update_RunWorkerCompleted;
      _justFilesCount = true;
      _update.RunWorkerAsync(_selectedFolderPath);
    }

    private void update_DoWork(object sender, DoWorkEventArgs e) {
      if (_justFilesCount) {
        _filesCount = 0;

        var paths = _selectedFolderPath.Equals(string.Empty)
          ? Settings.Default.CatalogFolders.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).ToList()
          : new List<string> { _selectedFolderPath };

        foreach (var path in paths) {
          _filesCount +=
            ACore.MediaItems.SuportedExts.Sum(
              ext => Directory.EnumerateFiles(path, ext.Replace(".", "*."), SearchOption.AllDirectories).Count());
        }

        return;
      }

      var worker = (BackgroundWorker) sender;
      _filesDone = 0;

      ProcessDirectory(_selectedFolderPath, worker);
      if (worker.CancellationPending) e.Cancel = true;
    }

    private void ProcessDirectory(string path, BackgroundWorker worker) {
      try {
        foreach (var dir in Directory.EnumerateDirectories(path)) {
          if (worker.CancellationPending) return;
          ProcessDirectory(dir, worker);
        }
      }
      catch (Exception) {
        // ignored
      }

      try {
        var dirId = ACore.Db.InsertDirectoryInToDb(path);

        foreach (var file in Directory.EnumerateFiles(path)
          .Where(f => ACore.MediaItems.SuportedExts.Any(x => f.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
          .OrderBy(x => x)) {

          var miInDb = ACore.Db.MediaItems.SingleOrDefault(x => x.DirectoryId == dirId && x.FileName == Path.GetFileName(file));
          var mi = miInDb != null
            ? new ViewModel.BaseMediaItem(file, miInDb)
            : new ViewModel.BaseMediaItem(file, new DataModel.MediaItem {
              Id = ACore.Db.GetNextIdFor<DataModel.MediaItem>(),
              FileName = Path.GetFileName(file),
              DirectoryId = dirId
            }, true);

          if (!_newOnly || mi.IsNew)
            mi.SaveMediaItemInToDb(!mi.IsNew, _lists);

          if (_rebuildThumbnails || !File.Exists(mi.FilePathCache))
            AppCore.CreateThumbnail(mi.FilePath, mi.FilePathCache, mi.ThumbSize);

          _filesDone++;

          worker.ReportProgress(Convert.ToInt32(((double)_filesDone / _filesCount) * 100));
        }
      }
      catch (Exception) {
        // ignored
      }
    }

    private void Update_ProgressChanged(object sender, ProgressChangedEventArgs e) {
      PbUpdateProgress.Value = e.ProgressPercentage;
      FilesProgress = $"{_filesDone} / {_filesCount}";
    }

    private void Update_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
      if (_justFilesCount) {
        _justFilesCount = false;
        _update.RunWorkerAsync();
        return;
      }
      ACore.Db.SubmitChanges(_lists);
      ACore.MediaItems.LoadAllItems();
      BtnUpdate.IsEnabled = true;
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      _update?.CancelAsync();
      Close();
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseRightButtonDown on StackPanel in TreeView
      e.Handled = true;
      var stackPanel = (StackPanel)sender;
      var item = stackPanel.DataContext;

      if (stackPanel.ContextMenu != null) return;
      var menu = new ContextMenu { Tag = item };

      switch (item) {
        case ViewModel.Folders _: {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderAdd"], CommandParameter = item});
          break;
        }
        case ViewModel.Folder f: {
          if (f.Parent == null) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRemove"], CommandParameter = item});
          }
          break;
        }
      }

      if (menu.Items.Count > 0)
        stackPanel.ContextMenu = menu;
    }

    private void LoadFolders() {
      var paths = Settings.Default.CatalogFolders.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
          .OrderBy(x => x).ToList();
      Folders.Items.Clear();
      foreach (var path in paths) {
        var di = new DirectoryInfo(path);
        var item = new ViewModel.Folder {
          Title = di.Name,
          FullPath = path,
          IconName = "appbar_folder",
          IsAccessible = true
        };
        try {
          if (di.GetDirectories().Length > 0)
            item.Items.Add(new ViewModel.Folder {Title = "..."});
        }
        catch (UnauthorizedAccessException) {
          item.IconName = "appbar_folder_lock";
          item.IsAccessible = false;
        }
        catch (DirectoryNotFoundException) {
          item.IconName = "appbar_folder_lock";
          item.IsAccessible = false;
        } finally {
          Folders.Items.Add(item);
        }
      }
    }

    private void CmdFolderAdd(object sender, ExecutedRoutedEventArgs e) {
      var dir = new FolderBrowserDialog();
      if (dir.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
      var paths = Settings.Default.CatalogFolders.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
      paths.Add(dir.SelectedPath);
      Settings.Default.CatalogFolders = string.Join(Environment.NewLine, paths);
      Settings.Default.Save();
      LoadFolders();
    }

    private void CmdFolderRemove(object sender, ExecutedRoutedEventArgs e) {
      var folder = e.Parameter as ViewModel.Folder;
      if (folder == null) return;
      var paths = Settings.Default.CatalogFolders.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
      paths.Remove(folder.FullPath);
      Settings.Default.CatalogFolders = string.Join(Environment.NewLine, paths);
      Settings.Default.Save();
      LoadFolders();
    }
  }
}
