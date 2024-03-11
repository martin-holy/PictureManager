using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PictureManager.Common;

public sealed class Settings {
  private static readonly string _filePath = "settings.json";
  
  [JsonIgnore]
  public bool Modified { get; set; }

  public CommonSettings Common { get; set; }
  public GeoNameSettings GeoName { get; set; }
  public ImagesToVideoSettings ImagesToVideo { get; set; }
  public MediaItemSettings MediaItem { get; set; }

  public void Save() {
    try {
      var opt = new JsonSerializerOptions { WriteIndented = true };
      File.WriteAllText(_filePath, JsonSerializer.Serialize(this, opt));
      Modified = false;
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public static Settings Load() {
    if (!File.Exists(_filePath)) return CreateNew();
    try {
      return JsonSerializer.Deserialize<Settings>(File.ReadAllText(_filePath)).Init();
    }
    catch (Exception ex) {
      Log.Error(ex);
      return CreateNew();
    }
  }

  public void OnClosing() {
    if (Modified &&
        Dialog.Show(new MessageDialog(
          "Settings changes",
          "There are some changes in settings. Do you want to save them?",
          Res.IconQuestion,
          true)) == 1)
      Save();
  }

  public static Settings CreateNew() =>
    new Settings {
      Common = new(),
      GeoName = new(),
      ImagesToVideo = new(),
      MediaItem = new()
    }.Init();

  public Settings Init() {
    AttachEvents(new ObservableObject[] { Common, GeoName, ImagesToVideo, MediaItem });
    return this;
  }

  private void AttachEvents(IEnumerable<ObservableObject> items) {
    foreach (var item in items)
      item.PropertyChanged += delegate { Modified = true; };
  }
}

public sealed class CommonSettings : ObservableObject {
  private string _cachePath = @":\Temp\PictureManagerCache";
  private string[] _directorySelectFolders;
  private string _ffmpegPath;
  private int _jpegQuality = 80;
  
  public string CachePath { get => _cachePath; set => OnCachePathChange(value); }
  public string[] DirectorySelectFolders { get => _directorySelectFolders; set { _directorySelectFolders = value; OnPropertyChanged(); } }
  public string FfmpegPath { get => _ffmpegPath; set { _ffmpegPath = value; OnPropertyChanged(); } }
  public int JpegQuality { get => _jpegQuality; set { _jpegQuality = value; OnPropertyChanged(); } }

  private void OnCachePathChange(string value) {
    if (value.Length < 4 || !value.StartsWith(":\\") || value.EndsWith("\\")) return;
    _cachePath = value;
    OnPropertyChanged(nameof(CachePath));
  }
}

public sealed class GeoNameSettings : ObservableObject {
  private bool _loadFromWeb;
  private string _userName;

  public bool LoadFromWeb { get => _loadFromWeb; set { _loadFromWeb = value; OnPropertyChanged(); } }
  public string UserName { get => _userName; set { _userName = value; OnPropertyChanged(); } }
}

public sealed class ImagesToVideoSettings : ObservableObject {
  private int _height = 1080;
  private int _quality = 27;
  private double _speed = 0.25;

  public int Height { get => _height; set { _height = value; OnPropertyChanged(); } }
  public int Quality { get => _quality; set { _quality = value; OnPropertyChanged(); } }
  public double Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }
}

public sealed class MediaItemSettings : ObservableObject {
  private double _mediaItemThumbScale = 0.8;
  private bool _scrollExactlyToMediaItem;
  private int _thumbSize = 400;
  private double _videoItemThumbScale = 0.28;
  
  public double MediaItemThumbScale { get => _mediaItemThumbScale; set { _mediaItemThumbScale = value; OnPropertyChanged(); } }
  public bool ScrollExactlyToMediaItem { get => _scrollExactlyToMediaItem; set { _scrollExactlyToMediaItem = value; OnPropertyChanged(); } }
  public int ThumbSize { get => _thumbSize; set { _thumbSize = value; OnPropertyChanged(); } }
  public double VideoItemThumbScale { get => _videoItemThumbScale; set { _videoItemThumbScale = value; OnPropertyChanged(); } }
}