using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WPicTransform.xaml
  /// </summary>
  public partial class WPicTransform : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _filesProgress;
    public string FilesProgress { get { return _filesProgress; } set { _filesProgress = value; OnPropertyChanged(); } }

    public WPicTransform() {
      InitializeComponent();
    }

    private void BtnUpdate_OnClick(object sender, RoutedEventArgs e) {
      /*_newOnly = ChbNewOnly.IsChecked == true;
      _rebuildThumbnails = ChbRebuildThumbs.IsChecked == true;

      var folder = TvFolders.SelectedItem as ViewModel.Folder;
      _selectedFolderPath = folder == null ? string.Empty : folder.FullPath;

      if (folder != null && !folder.IsAccessible) return;

      BtnUpdate.IsEnabled = false;
      _update = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
      _update.DoWork += update_DoWork;
      _update.ProgressChanged += update_ProgressChanged;
      _update.RunWorkerCompleted += update_RunWorkerCompleted;
      _justFilesCount = true;
      _update.RunWorkerAsync(_selectedFolderPath);*/
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      //_update.CancelAsync();
      Close();
    }
  }
}
