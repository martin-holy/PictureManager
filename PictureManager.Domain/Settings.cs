using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PictureManager.Domain; 

public sealed class Settings : ObservableObject {
  private bool _settingsChanged;
  private int _jpegQualityLevel = 80;
  private string _cachePath = ":\\Temp\\PictureManagerCache";
  private int _thumbnailSize = 400;
  private string _directorySelectFolders;
  private string _ffmpegPath;
  private int _imagesToVideoHeight = 1080;
  private int _imagesToVideoQuality = 27;
  private double _imagesToVideoSpeed = 0.25;
  private string _geoNamesUserName;
  private bool _loadGeoNamesFromWeb;
  private double _mediaItemThumbScale = 0.8;
  private double _videoItemThumbScale = 0.28;
  private bool _scrollExactlyToMediaItem;

  public int JpegQualityLevel { get => _jpegQualityLevel; set { _jpegQualityLevel = value; OnPropertyChanged(); } }
  public string CachePath { get => _cachePath; set => OnCachePathChange(value); }
  public int ThumbnailSize { get => _thumbnailSize; set { _thumbnailSize = value; OnPropertyChanged(); } }
  public string DirectorySelectFolders { get => _directorySelectFolders; set { _directorySelectFolders = value; OnPropertyChanged(); } }
  public string FfmpegPath { get => _ffmpegPath; set { _ffmpegPath = value; OnPropertyChanged(); } }
  public int ImagesToVideoHeight { get => _imagesToVideoHeight; set { _imagesToVideoHeight = value; OnPropertyChanged(); } }
  public int ImagesToVideoQuality { get => _imagesToVideoQuality; set { _imagesToVideoQuality = value; OnPropertyChanged(); } }
  public double ImagesToVideoSpeed { get => _imagesToVideoSpeed; set { _imagesToVideoSpeed = value; OnPropertyChanged(); } }
  public string GeoNamesUserName { get => _geoNamesUserName; set { _geoNamesUserName = value; OnPropertyChanged(); } }
  public bool LoadGeoNamesFromWeb { get => _loadGeoNamesFromWeb; set { _loadGeoNamesFromWeb = value; OnPropertyChanged(); } }
  public double MediaItemThumbScale { get => _mediaItemThumbScale; set { _mediaItemThumbScale = value; OnPropertyChanged(); } }
  public double VideoItemThumbScale { get => _videoItemThumbScale; set { _videoItemThumbScale = value; OnPropertyChanged(); } }
  public bool ScrollExactlyToMediaItem { get => _scrollExactlyToMediaItem; set { _scrollExactlyToMediaItem = value; OnPropertyChanged(); } }

  public string SettingsFileName { get; set; } = Path.Combine("db", "settings.csv");

  public Settings() {
    PropertyChanged += (_, _) => { _settingsChanged = true; };
  }

  public void OnClosing() {
    if (_settingsChanged &&
        Dialog.Show(new MessageDialog(
          "Settings changes",
          "There are some changes in settings. Do you want to save them?",
          Res.IconQuestion,
          true)) == 1)
      Save();
  }

  public bool Save() {
    try {
      using var sw = new StreamWriter(SettingsFileName, false, Encoding.UTF8, 65536);
      sw.WriteLine($"{nameof(JpegQualityLevel)}|{JpegQualityLevel}");
      sw.WriteLine($"{nameof(CachePath)}|{CachePath}");
      sw.WriteLine($"{nameof(ThumbnailSize)}|{ThumbnailSize}");
      sw.WriteLine($"{nameof(DirectorySelectFolders)}|{DirectorySelectFolders}");
      sw.WriteLine($"{nameof(FfmpegPath)}|{FfmpegPath}");
      sw.WriteLine($"{nameof(ImagesToVideoHeight)}|{ImagesToVideoHeight}");
      sw.WriteLine($"{nameof(ImagesToVideoQuality)}|{ImagesToVideoQuality}");
      sw.WriteLine($"{nameof(ImagesToVideoSpeed)}|{ImagesToVideoSpeed}");
      sw.WriteLine($"{nameof(GeoNamesUserName)}|{GeoNamesUserName}");
      sw.WriteLine($"{nameof(LoadGeoNamesFromWeb)}|{(LoadGeoNamesFromWeb ? 1 : 0)}");
      sw.WriteLine($"{nameof(MediaItemThumbScale)}|{MediaItemThumbScale}");
      sw.WriteLine($"{nameof(VideoItemThumbScale)}|{VideoItemThumbScale}");
      sw.WriteLine($"{nameof(ScrollExactlyToMediaItem)}|{(ScrollExactlyToMediaItem ? 1 : 0)}");

      _settingsChanged = false;
      return true;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return false;
    }
  }

  public bool Load() {
    if (!File.Exists(SettingsFileName)) return false;
    try {
      var props = new Dictionary<string, string>();
      using var sr = new StreamReader(SettingsFileName, Encoding.UTF8);

      while (sr.ReadLine() is { } line) {
        var prop = line.Split('|');
        if (prop.Length != 2)
          throw new ArgumentException("Incorrect number of values.", line);

        props.Add(prop[0], prop[1]);
      }

      if (props.TryGetValue(nameof(JpegQualityLevel), out var jpegQualityLevel))
        JpegQualityLevel = int.Parse(jpegQualityLevel);
      if (props.TryGetValue(nameof(CachePath), out var cachePath))
        CachePath = cachePath;
      if (props.TryGetValue(nameof(ThumbnailSize), out var thumbnailSize))
        ThumbnailSize = int.Parse(thumbnailSize);
      if (props.TryGetValue(nameof(DirectorySelectFolders), out var directorySelectFolders))
        DirectorySelectFolders = directorySelectFolders;
      if (props.TryGetValue(nameof(FfmpegPath), out var ffmpegPath))
        FfmpegPath = ffmpegPath;
      if (props.TryGetValue(nameof(ImagesToVideoHeight), out var imagesToVideoHeight))
        ImagesToVideoHeight = int.Parse(imagesToVideoHeight);
      if (props.TryGetValue(nameof(ImagesToVideoQuality), out var imagesToVideoQuality))
        ImagesToVideoQuality = int.Parse(imagesToVideoQuality);
      if (props.TryGetValue(nameof(ImagesToVideoSpeed), out var imagesToVideoSpeed))
        ImagesToVideoSpeed = double.Parse(imagesToVideoSpeed);
      if (props.TryGetValue(nameof(GeoNamesUserName), out var geoNamesUserName))
        GeoNamesUserName = geoNamesUserName;
      if (props.TryGetValue(nameof(LoadGeoNamesFromWeb), out var loadGeoNamesFromWeb))
        LoadGeoNamesFromWeb = "1".Equals(loadGeoNamesFromWeb);
      if (props.TryGetValue(nameof(MediaItemThumbScale), out var mediaItemThumbScale))
        MediaItemThumbScale = double.Parse(mediaItemThumbScale);
      if (props.TryGetValue(nameof(VideoItemThumbScale), out var videoItemThumbScale))
        VideoItemThumbScale = double.Parse(videoItemThumbScale);
      if (props.TryGetValue(nameof(ScrollExactlyToMediaItem), out var scrollExactlyToMediaItem))
        ScrollExactlyToMediaItem = "1".Equals(scrollExactlyToMediaItem);

      _settingsChanged = false;
      return true;
    }
    catch {
      // ignored
      return false;
    }
  }

  private void OnCachePathChange(string value) {
    if (value.Length < 4 || !value.StartsWith(":\\") || value.EndsWith("\\"))
      return;

    _cachePath = value;
    OnPropertyChanged(nameof(CachePath));
  }
}