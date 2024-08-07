using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Segment;

public sealed class ExportSegmentsDialog : Dialog {
  private readonly SegmentM[] _items;
  private readonly AsyncRelayCommand _exportCommand;
  private string _fileName = string.Empty;
  private string? _destDir;
  private int _progressMax;
  private int _progressValue;
  private readonly IProgress<(int, string)> _progress;

  public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
  public string? DestDir { get => _destDir; set { _destDir = value; OnPropertyChanged(); } }
  public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }
  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }

  public RelayCommand OpenFolderBrowserCommand { get; }

  public ExportSegmentsDialog(SegmentM[] items) : base("Export Segments", Res.IconSegment) {
    OpenFolderBrowserCommand = new(OpenFolderBrowser, Res.IconFolder, "Select folder");
    _exportCommand = new(Export, () => !string.IsNullOrEmpty(_destDir), null, "Export");
    _items = items;
    _progressMax = _items.Length;
    _progress = new Progress<(int, string)>(x => {
      ProgressValue = x.Item1;
      FileName = x.Item2;
    });

    Buttons = [
      new(_exportCommand, true),
      new(CloseCommand, false, true)
    ];
  }

  public override Task OnResultChanged(int result) {
    if (result == 0) _exportCommand.CancelCommand.Execute(null);
    return Task.CompletedTask;
  }

  private async Task Export(CancellationToken token) {
    try {
      if (!Directory.Exists(_destDir))
        Directory.CreateDirectory(_destDir!);

      await Task.Run(() => {
        var index = 0;
        var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = token };

        Parallel.ForEach(_items, po, s => {
          var fileName = s.MediaItem.FileName.Replace(".jpg", "_segment_" + s.Id + ".jpg", StringComparison.OrdinalIgnoreCase);
          _progress.Report((index++, fileName));

          try {
            SegmentS.ExportSegment(s, Path.Combine(_destDir!, fileName));
          }
          catch (Exception ex) {
            Log.Error(ex, s.MediaItem.FilePath);
          }
        });
      }, token);
    }
    finally {
      Result = 1;
    }
  }

  private void OpenFolderBrowser() {
    if (Core.VM.BrowseForFolder() is { } dirPath)
      _destDir = dirPath;
  }
}