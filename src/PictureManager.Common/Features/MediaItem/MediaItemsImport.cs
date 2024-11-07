using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemsImport : ObservableObject {
  private readonly IProgress<int> _progress;
  private readonly WorkTask _task = new();

  private bool _isImporting;
  private int _count;
  private int _doneCount;

  public bool IsImporting { get => _isImporting; set { _isImporting = value; OnPropertyChanged(); } }
  public int Count { get => _count; set { _count = value; OnPropertyChanged(); } }
  public int DoneCount { get => _doneCount; set { _doneCount = value; OnPropertyChanged(); } }

  public RelayCommand CancelCommand { get; }

  public MediaItemsImport() {
    _progress = new Progress<int>(x => DoneCount += x);
    CancelCommand = new(CancelImport, null, "Cancel");
  }

  public async Task Import(List<MediaItemMetadata> items) {
    if (items.Count == 0) return;
    IsImporting = true;
    Count = items.Count;
    DoneCount = 0;

    try {
      await _task.Start(new(() => ReadMetadata(items)));

      Tasks.Dispatch(delegate { DoneCount = 0; }); // new counter for loading GeoNames if any

      foreach (var mim in items) {
        if (mim.Success)
          await mim.FindRefs();
        else
          Core.R.MediaItem.ItemDelete(mim.MediaItem);

        Tasks.Dispatch(delegate { DoneCount++; });
      }
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
    finally {
      IsImporting = false;
    }
  }

  private void ReadMetadata(List<MediaItemMetadata> items) {
    try {
      var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _task.Token };
      Parallel.ForEach(items.Where(x => x.MediaItem is ImageM), po, mim => {
        MediaItemS.ReadMetadata(mim, false);
        _progress.Report(1);
      });
    }
    catch (OperationCanceledException) { }

    foreach (var mim in items.Where(x => x.MediaItem is VideoM)) {
      if (_task.Token.IsCancellationRequested) break;
      Tasks.RunOnUiThread(() => {
        MediaItemS.ReadMetadata(mim, false);
        Tasks.Dispatch(delegate { DoneCount++; });
      }).Wait();
    }
  }

  private async void CancelImport() =>
    await _task.Cancel();
}