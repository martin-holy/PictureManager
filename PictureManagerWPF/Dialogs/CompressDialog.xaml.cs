using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MH.Utils.Extensions;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.Dialogs {
  public partial class CompressDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));

    private CancellationTokenSource _cts;
    private Task _workTask;

    private int _jpegQualityLevel;
    private long _totalSourceSize;
    private long _totalCompressedSize;

    public int JpegQualityLevel { get => _jpegQualityLevel; set { _jpegQualityLevel = value; OnPropertyChanged(); } }
    public string TotalSourceSize => IOExtensions.FileSizeToString(_totalSourceSize);
    public string TotalCompressedSize => IOExtensions.FileSizeToString(_totalCompressedSize);

    public CompressDialog() {
      InitializeComponent();
      JpegQualityLevel = Properties.Settings.Default.JpegQualityLevel;
    }

    private static async IAsyncEnumerable<long[]> CompressMediaItemsAsync(List<MediaItemM> items, [EnumeratorCancellation] CancellationToken token = default) {
      foreach (var mi in items) {
        if (token.IsCancellationRequested) yield break;

        yield return await Task.Run(() => {
          var originalSize = new FileInfo(mi.FilePath).Length;
          var bSuccess = App.Ui.MediaItemsVM.TryWriteMetadata(mi);
          var newSize = bSuccess ? new FileInfo(mi.FilePath).Length : originalSize;
          return new[] { originalSize, newSize };
        });
      }
    }

    private async Task Compress() {
      var items = App.Core.ThumbnailsGridsM.Current.FilteredItems.Where(x =>
        x.MediaType == MediaType.Image && (OptSelected.IsChecked != true || x.IsSelected)).ToList();

      PbCompressProgress.Maximum = items.Count;
      PbCompressProgress.Value = 0;
      _totalSourceSize = 0;
      _totalCompressedSize = 0;
      OnPropertyChanged(nameof(TotalSourceSize));
      OnPropertyChanged(nameof(TotalCompressedSize));

      _cts?.Dispose();
      _cts = new();

      await foreach (var fileSizes in CompressMediaItemsAsync(items, _cts.Token)) {
        PbCompressProgress.Value++;
        _totalSourceSize += fileSizes[0];
        _totalCompressedSize += fileSizes[1];
        OnPropertyChanged(nameof(TotalSourceSize));
        OnPropertyChanged(nameof(TotalCompressedSize));
      }

      _cts?.Dispose();
      _cts = null;
    }

    private async void BtnCompress_OnClick(object sender, RoutedEventArgs e) {
      GbSettings.IsEnabled = false;
      BtnCompress.IsEnabled = false;
      BtnCancel.Content = "Cancel";

      _workTask = Compress();
      await _workTask;

      GbSettings.IsEnabled = true;
      BtnCompress.IsEnabled = true;
      BtnCancel.Content = "Close";
    }

    private async void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      _cts?.Cancel();
      if (_workTask != null)
        await _workTask;
      Close();
    }

    public static void Open() {
      var compress = new CompressDialog { Owner = Application.Current.MainWindow };
      compress.ShowDialog();
    }
  }
}
