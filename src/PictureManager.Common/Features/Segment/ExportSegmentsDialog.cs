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
  public RelayCommand OpenFolderBrowserCommand { get; }

  public ExportSegmentsDialog(SegmentM[] items) : base("Export Segments", Res.IconSegment, items, null, "Export") {
    OpenFolderBrowserCommand = new(() => DestDir = Core.VM.BrowseForFolder(), Res.IconFolder, "Select folder");
  }

  protected override bool CanAction() =>
    !string.IsNullOrEmpty(_destDir);

  protected override bool DoBefore() {
    Directory.CreateDirectory(_destDir!);
    return true;
  }

  protected override Task Do(SegmentM segment, CancellationToken token) {
    var fileName = segment.MediaItem.FileName.Replace(".jpg", "_segment_" + segment.Id + ".jpg", StringComparison.OrdinalIgnoreCase);
    ReportProgress(fileName);

    try {
      SegmentS.ExportSegment(segment, Path.Combine(_destDir!, fileName));
    }
    catch (Exception ex) {
      Log.Error(ex, segment.MediaItem.FilePath);
    }

    return Task.CompletedTask;
  }
}