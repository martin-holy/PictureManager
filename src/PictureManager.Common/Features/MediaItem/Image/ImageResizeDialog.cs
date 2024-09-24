using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ImageResizeDialog : ParallelProgressDialog<ImageM> {
  private bool _preserveThumbnail;
  private bool _preserveMetadata;
  private string? _destDir;
  private double _mpx;
  private double _maxMpx;
  private int _px;

  public bool PreserveThumbnail { get => _preserveThumbnail; set { _preserveThumbnail = value; OnPropertyChanged(); } }
  public bool PreserveMetadata { get => _preserveMetadata; set { _preserveMetadata = value; OnPropertyChanged(); } }
  public string? DestDir { get => _destDir; set { _destDir = value; OnPropertyChanged(); } }
  public double Mpx { get => _mpx; set { _mpx = value; OnPropertyChanged(); } }
  public double MaxMpx { get => _maxMpx; set { _maxMpx = value; OnPropertyChanged(); } }

  public RelayCommand OpenFolderBrowserCommand { get; }

  public ImageResizeDialog(ImageM[] items) : base("Resize Images", Res.IconImageMultiple, items, null, "Resize") {
    OpenFolderBrowserCommand = new(() => DestDir = CoreVM.BrowseForFolder(), Res.IconFolder, "Select folder");
    SetMaxMpx(items);
  }

  protected override bool CanAction() =>
    !string.IsNullOrEmpty(_destDir);

  protected override bool DoBefore() {
    Directory.CreateDirectory(_destDir!);
    _px = Convert.ToInt32(Mpx * 1000000);
    return true;
  }

  protected override Task Do(ImageM image, CancellationToken token) {
    ReportProgress(image.FileName);

    try {
      var dest = Path.Combine(_destDir!, image.FileName);
      Imaging.ResizeJpg(image.FilePath, dest, _px, _preserveMetadata, _preserveThumbnail, Core.Settings.Common.JpegQuality);
    }
    catch (Exception ex) {
      Log.Error(ex, image.FilePath);
    }

    return Task.CompletedTask;
  }

  private void SetMaxMpx(ImageM[] items) {
    var maxPx = 0;
    foreach (var mi in items) {
      var px = mi.Width * mi.Height;
      if (px > maxPx) maxPx = px;
    }

    MaxMpx = Math.Round(maxPx / 1000000.0, 1);
    Mpx = MaxMpx;
  }
}