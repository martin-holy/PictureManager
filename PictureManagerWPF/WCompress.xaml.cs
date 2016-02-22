using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using PictureManager.ShellStuff;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WCompress.xaml
  /// </summary>
  public partial class WCompress : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private AppCore _appCore;
    private BackgroundWorker _compress;

    private int _jpegQualityLevel;
    private long _totalSourceSize;
    private long _totalCompressedSize;

    public int JpegQualityLevel {
      get { return _jpegQualityLevel; }
      set {
        _jpegQualityLevel = value;
        OnPropertyChanged();
      }
    }

    public string TotalSourceSize => FormatSize(_totalSourceSize);
    public string TotalCompressedSize => FormatSize(_totalCompressedSize);

    public WCompress(AppCore appCore) {
      InitializeComponent();
      _appCore = appCore;
      JpegQualityLevel = Properties.Settings.Default.JpegQualityLevel;
    }

    public string FormatSize(long size) {
      string[] sizes = { "B", "KB", "MB", "GB" };
      int order = 0;
      while (size >= 1024 && order + 1 < sizes.Length) {
        order++;
        size = size / 1024;
      }

      return $"{size:0.##} {sizes[order]}";
    }

    private void BtnCompress_OnClick(object sender, RoutedEventArgs e) {
      GbSettings.IsEnabled = false;
      BtnCompress.IsEnabled = false;

      PbCompressProgress.Value = 0;
      _totalSourceSize = 0;
      _totalCompressedSize = 0;
      OnPropertyChanged("TotalSourceSize");
      OnPropertyChanged("TotalCompressedSize");

      _compress = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
      _compress.DoWork += compress_DoWork;
      _compress.ProgressChanged += compress_ProgressChanged;
      _compress.RunWorkerCompleted += compress_RunWorkerCompleted;
      _compress.RunWorkerAsync(OptSelected.IsChecked != null && OptSelected.IsChecked.Value
        ? _appCore.MediaItems.Items.Where(x => x.IsSelected)
        : _appCore.MediaItems.Items);
    }

    private void compress_DoWork(object sender, DoWorkEventArgs e) {
      var worker = (BackgroundWorker) sender;
      var pictures = (ObservableCollection<Data.Picture>) e.Argument;
      var count = pictures.Count;
      var done = 0;
      const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      foreach (var picture in pictures) {
        if (worker.CancellationPending) {
          e.Cancel = true;
          break;
        }

        FileInfo original = new FileInfo(picture.FilePath);
        FileInfo newFile = new FileInfo(picture.FilePath.Replace(".", "_newFile."));
        bool bSuccess = false;

        try {
          using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = _jpegQualityLevel };
            //BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile and BitmapCacheOption.None
            //is a KEY to lossless jpeg edit if the QualityLevel is the same
            encoder.Frames.Add(BitmapFrame.Create(originalFileStream, createOptions, BitmapCacheOption.None));

            using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
              encoder.Save(newFileStream);
            }

            bSuccess = true;
          }
        }
        catch (Exception) {
          // ignored
        }

        if (bSuccess) {
          try {
            long[] fileSizes = { original.Length, newFile.Length };
            done++;

            newFile.CreationTime = original.CreationTime;

            using (FileOperation fo = new FileOperation()) {
              var flags = FileOperationFlags.FOF_SILENT | FileOperationFlags.FOFX_RECYCLEONDELETE | FileOperationFlags.FOF_NOERRORUI;
              fo.SetOperationFlags(flags);
              fo.DeleteItem(original.FullName);
              fo.PerformOperations();
            }

            newFile.MoveTo(original.FullName);
            
            worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100), fileSizes);
          }
          catch (Exception) {
            // ignored
          }
        }
      }
    }

    private void compress_ProgressChanged(object sender, ProgressChangedEventArgs e) {
      PbCompressProgress.Value = e.ProgressPercentage;
      if (e.UserState == null) return;
      _totalSourceSize += ((long[]) e.UserState)[0];
      _totalCompressedSize += ((long[])e.UserState)[1];
      OnPropertyChanged("TotalSourceSize");
      OnPropertyChanged("TotalCompressedSize");
    }

    private void compress_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
      GbSettings.IsEnabled = true;
      BtnCompress.IsEnabled = true;
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      _compress.CancelAsync();
      Close();
    }
  }
}
