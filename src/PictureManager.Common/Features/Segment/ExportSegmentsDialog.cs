﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Segment;

public sealed class ExportSegmentsDialog : Dialog {
  private CancellationTokenSource? _cts;
  private readonly SegmentM[] _items;
  private string _fileName = string.Empty;
  private string? _destDir;
  private int _progressMax;
  private int _progressValue;

  public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
  public string? DestDir { get => _destDir; set { _destDir = value; OnPropertyChanged(); } }
  public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }
  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }

  public RelayCommand OpenFolderBrowserCommand { get; }

  public ExportSegmentsDialog(SegmentM[] items) : base("Export Segments", Res.IconSegment) {
    OpenFolderBrowserCommand = new(OpenFolderBrowser, Res.IconFolder, "Select folder");
    Buttons = [
      new(new(Export, () => !string.IsNullOrEmpty(DestDir), null, "Export"), true),
      new(CloseCommand, false, true)
    ]; 
    _items = items;
    ProgressMax = _items.Length;
  }

  public override Task OnResultChanged(int result) {
    if (result == 0 && _cts != null) _cts.Cancel();
    return Task.CompletedTask;
  }

  private void Export() {
    if (!Directory.Exists(DestDir)) {
      try {
        Directory.CreateDirectory(DestDir!);
      }
      catch (Exception ex) {
        Log.Error(ex);
        return;
      }
    }

    ExportSegments(DestDir!);
  }

  private async void ExportSegments(string destination) {
    _cts = new();

    await Task.Run(() => {
      try {
        var index = 0;
        var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _cts.Token };

        Parallel.ForEach(_items, po, s => {
          index++;
          var fileName = s.MediaItem.FileName.Replace(".jpg", "_segment_" + s.Id + ".jpg", StringComparison.OrdinalIgnoreCase);

          Tasks.RunOnUiThread(() => {
            ProgressValue = index;
            FileName = fileName;
          });

          try {
            var dest = Path.Combine(destination, fileName);
            SegmentS.ExportSegment(s, dest);
          }
          catch (Exception ex) {
            Log.Error(ex, s.MediaItem.FilePath);
          }
        });
      }
      catch (OperationCanceledException) { }
      finally {
        _cts.Dispose();
        _cts = null;
        Tasks.RunOnUiThread(() => { Result = 1; });
      }
    });
  }

  private void OpenFolderBrowser() {
    if (Core.VM.BrowseForFolder() is { } dirPath)
      DestDir = dirPath;
  }
}