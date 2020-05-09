using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.ViewModels;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for WCompress.xaml
  /// </summary>
  public partial class CompressDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private BackgroundWorker _compress;

    private int _jpegQualityLevel;
    private long _totalSourceSize;
    private long _totalCompressedSize;

    public int JpegQualityLevel { get => _jpegQualityLevel; set { _jpegQualityLevel = value; OnPropertyChanged(); } }
    public string TotalSourceSize => Extensions.FileSizeToString(_totalSourceSize);
    public string TotalCompressedSize => Extensions.FileSizeToString(_totalCompressedSize);

    public CompressDialog() {
      InitializeComponent();
      JpegQualityLevel = Properties.Settings.Default.JpegQualityLevel;
    }

    private void BtnCompress_OnClick(object sender, RoutedEventArgs e) {
      GbSettings.IsEnabled = false;
      BtnCompress.IsEnabled = false;
      BtnCancel.Content = "Cancel";

      PbCompressProgress.Value = 0;
      _totalSourceSize = 0;
      _totalCompressedSize = 0;
      OnPropertyChanged(nameof(TotalSourceSize));
      OnPropertyChanged(nameof(TotalCompressedSize));

      _compress = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
      _compress.DoWork += Compress_DoWork;
      _compress.ProgressChanged += Compress_ProgressChanged;
      _compress.RunWorkerCompleted += Compress_RunWorkerCompleted;
      _compress.RunWorkerAsync(OptSelected.IsChecked != null && OptSelected.IsChecked.Value
        ? App.Core.Model.MediaItems.FilteredItems.Where(x => x.IsSelected && x.MediaType == MediaType.Image).ToList()
        : App.Core.Model.MediaItems.FilteredItems.ToList());
    }

    private static void Compress_DoWork(object sender, DoWorkEventArgs e) {
      var worker = (BackgroundWorker) sender;
      var mis = (List<MediaItem>) e.Argument;
      var count = mis.Count;
      var done = 0;

      foreach (var mi in mis) {
        if (worker.CancellationPending) {
          e.Cancel = true;
          break;
        }

        var originalSize = new FileInfo(mi.FilePath).Length;
        var bSuccess = MediaItemsViewModel.TryWriteMetadata(mi);
        var newSize = bSuccess ? new FileInfo(mi.FilePath).Length : originalSize;
        long[] fileSizes = {originalSize, newSize};
        done++;
        worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100), fileSizes);
      }
    }

    private void Compress_ProgressChanged(object sender, ProgressChangedEventArgs e) {
      PbCompressProgress.Value = e.ProgressPercentage;
      if (e.UserState == null) return;
      _totalSourceSize += ((long[]) e.UserState)[0];
      _totalCompressedSize += ((long[])e.UserState)[1];
      OnPropertyChanged(nameof(TotalSourceSize));
      OnPropertyChanged(nameof(TotalCompressedSize));
    }

    private void Compress_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
      GbSettings.IsEnabled = true;
      BtnCompress.IsEnabled = true;
      BtnCancel.Content = "Close";
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      _compress.CancelAsync();
      Close();
    }

    public static void ShowDialog(Window owner) {
      var compress = new CompressDialog {Owner = owner};
      compress.ShowDialog();
    }
  }
}
