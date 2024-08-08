using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ImageResizeDialog : Dialog {
  private readonly ImageM[] _items;
  private readonly AsyncRelayCommand _resizeCommand;
  private bool _preserveThumbnail;
  private bool _preserveMetadata;
  private string _fileName = string.Empty;
  private string? _destDir;
  private double _mpx;
  private double _maxMpx;
  private int _progressMax;
  private int _progressValue;
  private readonly IProgress<(int, string)> _progress;

  public bool PreserveThumbnail { get => _preserveThumbnail; set { _preserveThumbnail = value; OnPropertyChanged(); } }
  public bool PreserveMetadata { get => _preserveMetadata; set { _preserveMetadata = value; OnPropertyChanged(); } }
  public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
  public string? DestDir { get => _destDir; set { _destDir = value; OnPropertyChanged(); } }
  public double Mpx { get => _mpx; set { _mpx = value; OnPropertyChanged(); } }
  public double MaxMpx { get => _maxMpx; set { _maxMpx = value; OnPropertyChanged(); } }
  public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }
  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }

  public RelayCommand OpenFolderBrowserCommand { get; }

  public ImageResizeDialog(ImageM[] items) : base("Resize Images", Res.IconImageMultiple) {
    OpenFolderBrowserCommand = new(OpenFolderBrowser, Res.IconFolder, "Select folder");
    _resizeCommand = new(Resize, () => !string.IsNullOrEmpty(_destDir), null, "Resize");
    _items = items;
    _progressMax = _items.Length;
    _progress = new Progress<(int, string)>(x => {
      ProgressValue = x.Item1;
      FileName = x.Item2;
    });

    Buttons = [
      new(_resizeCommand, true),
      new(CloseCommand, false, true)
    ]; 
    
    SetMaxMpx();
  }

  protected override Task OnResultChanged(int result) {
    if (result == 0) _resizeCommand.CancelCommand.Execute(null);
    return Task.CompletedTask;
  }

  private void SetMaxMpx() {
    var maxPx = 0;
    foreach (var mi in _items) {
      var px = mi.Width * mi.Height;
      if (px > maxPx) maxPx = px;
    }

    MaxMpx = Math.Round(maxPx / 1000000.0, 1);
    Mpx = MaxMpx;
  }

  private async Task Resize(CancellationToken token) {
    var px = Convert.ToInt32(Mpx * 1000000);

    try {
      if (!Directory.Exists(_destDir))
        Directory.CreateDirectory(_destDir!);

      await Task.Run(() => {
        var index = 0;
        var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = token };

        Parallel.ForEach(_items, po, mi => {
          _progress.Report((index++, mi.FileName));

          try {
            var dest = Path.Combine(_destDir!, mi.FileName);
            Imaging.ResizeJpg(mi.FilePath, dest, px, _preserveMetadata, _preserveThumbnail, Core.Settings.Common.JpegQuality);
          }
          catch (Exception ex) {
            Log.Error(ex, mi.FilePath);
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