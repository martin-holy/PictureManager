using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ImageResizeDialog : ParallelProgressDialog<ImageM> {
  private bool _preserveThumbnail;
  private bool _preserveMetadata;
  private bool _preserveFolders;
  private bool _skipIfExists = true;
  private string? _destDir;
  private double _mpx;
  private double _maxMpx;
  private int _px;
  private Dictionary<ImageM, string>? _pathMap;

  public bool PreserveThumbnail { get => _preserveThumbnail; set { _preserveThumbnail = value; OnPropertyChanged(); } }
  public bool PreserveMetadata { get => _preserveMetadata; set { _preserveMetadata = value; OnPropertyChanged(); } }
  public bool PreserveFolders { get => _preserveFolders; set { _preserveFolders = value; OnPropertyChanged(); } }
  public bool SkipIfExists { get => _skipIfExists; set { _skipIfExists = value; OnPropertyChanged(); } }
  public string? DestDir { get => _destDir; set { _destDir = value; OnPropertyChanged(); } }
  public double Mpx { get => _mpx; set { _mpx = value; OnPropertyChanged(); } }
  public double MaxMpx { get => _maxMpx; set { _maxMpx = value; OnPropertyChanged(); } }

  public AsyncRelayCommand OpenFolderBrowserCommand { get; }

  public ImageResizeDialog(ImageM[] items) : base("Resize Images", Res.IconImageMultiple, items, null, "Resize") {
    OpenFolderBrowserCommand = new(_openFolderBrowser, Res.IconFolder, "Select folder");
    _setMaxMpx(items);
  }

  protected override bool _canAction() =>
    !string.IsNullOrEmpty(_destDir);

  protected override bool _doBefore() {
    _px = Convert.ToInt32(Mpx * 1000000);
    _pathMap = _buildPathMap(_items, _destDir!);

    foreach (var dir in _pathMap.Values.Select(Path.GetDirectoryName).Distinct()!)
      Directory.CreateDirectory(dir!);
    
    return true;
  }

  protected override void _doAfter() {
    base._doAfter();
    _pathMap?.Clear();
  }

  protected override Task _do(ImageM image, CancellationToken token) {
    _reportProgress(image.FileName);

    try {
      var dest = _pathMap![image];

      if (File.Exists(dest)) {
        if (_skipIfExists) return Task.CompletedTask;
        File.Delete(dest);
      }

      Imaging.ResizeJpg(image.FilePath, dest, _px, _preserveMetadata, _preserveThumbnail, Core.Settings.Common.JpegQuality);
    }
    catch (Exception ex) {
      Log.Error(ex, image.FilePath);
    }

    return Task.CompletedTask;
  }

  private static Dictionary<ImageM, string> _buildPathMap(ImageM[] files, string destinationRoot) {
    var commonRoot = _getCommonRoot(files.Select(x => x.FilePath).ToArray());
    var map = new Dictionary<ImageM, string>();

    foreach (var file in files) {
      var relative = Path.GetRelativePath(commonRoot, file.FilePath);
      var output = Path.Combine(destinationRoot, relative);
      map[file] = output;
    }

    return map;
  }

  private static string _getCommonRoot(string[] paths) {
    if (paths.Length == 0) return String.Empty;

    var separated = paths
      .Select(x => Path
        .GetFullPath(x)
        .TrimEnd(Path.DirectorySeparatorChar)
        .Split(Path.DirectorySeparatorChar))
      .ToArray();

    var minLen = separated.Min(parts => parts.Length);
    var lastCommon = 0;

    for (var i = 1; i < minLen; i++) // start at index 1 -> skip drive letter
      if (separated.All(parts => parts[i].Equals(separated[0][i], StringComparison.OrdinalIgnoreCase)))
        lastCommon = i;
      else
        break;

    // Join from index 1 to lastCommon to drop drive
    return string.Join(Path.DirectorySeparatorChar, separated[0].Take(lastCommon + 1));
  }

  private void _setMaxMpx(ImageM[] items) {
    var maxPx = 0;
    foreach (var mi in items) {
      var px = mi.Width * mi.Height;
      if (px > maxPx) maxPx = px;
    }

    MaxMpx = Math.Round(maxPx / 1000000.0, 1);
    Mpx = MaxMpx;
  }

  private async Task _openFolderBrowser(CancellationToken token) {
    DestDir = await CoreVM.BrowseForFolder();
  }
}