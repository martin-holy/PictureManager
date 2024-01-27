using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Dialogs {
    public class CompressDialogM : Dialog {
    private CancellationTokenSource _cts;
    private Task _workTask;

    private int _jpegQualityLevel;
    private long _totalSourceSize;
    private long _totalCompressedSize;
    private double _progressValue;
    private bool _isWorkInProgress;

    public int JpegQualityLevel { get => _jpegQualityLevel; set { _jpegQualityLevel = value; OnPropertyChanged(); } }
    public string TotalSourceSize => IOExtensions.FileSizeToString(_totalSourceSize);
    public string TotalCompressedSize => IOExtensions.FileSizeToString(_totalCompressedSize);
    public double ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
    public bool IsWorkInProgress { get => _isWorkInProgress; set { _isWorkInProgress = value; OnPropertyChanged(); } }
    public ImageM[] Items { get; set; }

    public CompressDialogM(ImageM[] items, int jpegQualityLevel) : base("Compress Pictures to JPG", Res.IconImage) {
      Items = items;
      JpegQualityLevel = jpegQualityLevel;
      ProgressValue = Items.Length;

      var compressCommand = new RelayCommand<object>(
        async () => { await Compress(); },
        () => !IsWorkInProgress);

      CloseCommand = new(async () => { await Cancel(); });

      Buttons = new DialogButton[] {
        new("Compress", Res.IconImage, compressCommand, true, false),
        new("Close", Res.IconXCross, CloseCommand, false, true) };
    }

    public async Task Cancel() {
      _cts?.Cancel();
      if (_workTask != null)
        await _workTask;

      Result = 0;
    }

    public async Task Compress() {
      IsWorkInProgress = true;
      
      _workTask = Task.Run(async () => {
        ProgressValue = 0;
        _totalSourceSize = 0;
        _totalCompressedSize = 0;
        OnPropertyChanged(nameof(TotalSourceSize));
        OnPropertyChanged(nameof(TotalCompressedSize));

        _cts?.Dispose();
        _cts = new();

        await foreach (var fileSizes in CompressMediaItemsAsync(Items, _cts.Token)) {
          ProgressValue++;
          _totalSourceSize += fileSizes[0];
          _totalCompressedSize += fileSizes[1];
          OnPropertyChanged(nameof(TotalSourceSize));
          OnPropertyChanged(nameof(TotalCompressedSize));
        }

        _cts?.Dispose();
        _cts = null;
      });

      await _workTask;
      IsWorkInProgress = false;
      // this is here because button doesn't get updated until focus is changed
      RelayCommand.InvokeCanExecuteChanged(null, EventArgs.Empty);
    }

    private static async IAsyncEnumerable<long[]> CompressMediaItemsAsync(ImageM[] items, [EnumeratorCancellation] CancellationToken token = default) {
      foreach (var mi in items) {
        if (token.IsCancellationRequested) yield break;

        yield return await Task.Run(() => {
          var originalSize = new FileInfo(mi.FilePath).Length;
          var bSuccess = Core.VM.TryWriteMetadata(mi);
          var newSize = bSuccess ? new FileInfo(mi.FilePath).Length : originalSize;
          return new[] { originalSize, newSize };
        });
      }
    }
  }
}
