using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.Segment;

public sealed class ExportSegmentsDialog : ParallelProgressDialog<SegmentM> {
  private string? _destDir;

  public string? DestDir { get => _destDir; set { _destDir = value; OnPropertyChanged(); } }
  public AsyncRelayCommand? OpenFolderBrowserCommand { get; }

  public ExportSegmentsDialog(SegmentM[] items) : base("Export Segments", Res.IconSegment, items, null, "Export") {
    OpenFolderBrowserCommand = new(_openFolderBrowser, Res.IconFolder, "Select folder");
  }

  public ExportSegmentsDialog(SegmentM[] items, string destDir) : base("Export Segments", Res.IconSegment, items) {
    _destDir = destDir;
    _autoRun();
  }

  protected override bool _canAction() =>
    !string.IsNullOrEmpty(_destDir);

  protected override bool _doBefore() {
    Directory.CreateDirectory(_destDir!);
    return true;
  }

  protected override Task _do(SegmentM segment, CancellationToken token) {
    var fileName = segment.MediaItem.FileName.Replace(".jpg", "_segment_" + segment.Id + ".jpg", StringComparison.OrdinalIgnoreCase);
    _reportProgress(fileName);

    try {
      SegmentS.ExportSegment(segment, Path.Combine(_destDir!, fileName));
    }
    catch (Exception ex) {
      Log.Error(ex, segment.MediaItem.FilePath);
    }

    return Task.CompletedTask;
  }

  private async Task _openFolderBrowser(CancellationToken token) {
    DestDir = await CoreVM.BrowseForFolder();
  }
}