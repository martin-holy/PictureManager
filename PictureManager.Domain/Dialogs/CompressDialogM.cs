using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Dialogs {
  public class CompressDialogM : ObservableObject, IDialog {
    private CancellationTokenSource _cts;
    private Task _workTask;

    private string _title;
    private int _result = -1;
    private int _jpegQualityLevel;
    private long _totalSourceSize;
    private long _totalCompressedSize;
    private double _progressValue;
    private bool _isWorkInProgress;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public int JpegQualityLevel { get => _jpegQualityLevel; set { _jpegQualityLevel = value; OnPropertyChanged(); } }
    public string TotalSourceSize => IOExtensions.FileSizeToString(_totalSourceSize);
    public string TotalCompressedSize => IOExtensions.FileSizeToString(_totalCompressedSize);
    public double ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
    public bool IsWorkInProgress { get => _isWorkInProgress; set { _isWorkInProgress = value; OnPropertyChanged(); } }
    public List<MediaItemM> Items { get; set; }

    public RelayCommand<object> CancelCommand { get; set; }
    public RelayCommand<object> CompressCommand { get; set; }

    public CompressDialogM(List<MediaItemM> items, int jpegQualityLevel) {
      Title = "Compress Pictures to JPG";
      Items = items;
      JpegQualityLevel = jpegQualityLevel;
      ProgressValue = Items.Count;

      CancelCommand = new(
        async () => {
          await Cancel();
          Result = 0; // close
        },
        () => IsWorkInProgress);

      CompressCommand = new(
        async () => { await Compress(); },
        () => !IsWorkInProgress);
    }

    public async Task Cancel() {
      _cts?.Cancel();
      if (_workTask != null)
        await _workTask;
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

    private static async IAsyncEnumerable<long[]> CompressMediaItemsAsync(List<MediaItemM> items, [EnumeratorCancellation] CancellationToken token = default) {
      foreach (var mi in items) {
        if (token.IsCancellationRequested) yield break;

        yield return await Task.Run(() => {
          var originalSize = new FileInfo(mi.FilePath).Length;
          var bSuccess = Core.Instance.MediaItemsM.TryWriteMetadata(mi);
          var newSize = bSuccess ? new FileInfo(mi.FilePath).Length : originalSize;
          return new[] { originalSize, newSize };
        });
      }
    }
  }
}
