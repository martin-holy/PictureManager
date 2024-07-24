using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PictureManager.Common.Features.Common;

public sealed class Settings {
  private const string _filePath = "settings.json";

  [JsonIgnore]
  public bool Modified { get; set; }
  [JsonIgnore]
  public ListItem[] Groups { get; }

  public CommonSettings Common { get; }
  public GeoNameSettings GeoName { get; }
  public ImagesToVideoSettings ImagesToVideo { get; }
  public MediaItemSettings MediaItem { get; }
  public SegmentSettings Segment { get; }

  public Settings(CommonSettings common, GeoNameSettings geoName, ImagesToVideoSettings imagesToVideo, MediaItemSettings mediaItem, SegmentSettings segment) {
    Common = common;
    GeoName = geoName;
    ImagesToVideo = imagesToVideo;
    MediaItem = mediaItem;
    Segment = segment;

    Groups = [
      new(Res.IconSettings, "Common", common),
      new(Res.IconLocationCheckin, "GeoName", geoName),
      //new(Res.IconBug, "Images to video", imagesToVideo),
      new(Res.IconImageMultiple, "MediaItem", mediaItem),
      new(Res.IconSegment, "Segment", segment)
    ];

    foreach (var item in Groups.Select(x => (ObservableObject)x.Data!))
      item.PropertyChanged += delegate { Modified = true; };
  }

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
      using var doc = JsonDocument.Parse(File.ReadAllText(_filePath));
      var root = doc.RootElement;

      var common = DeserializeGroup<CommonSettings>(root, "Common") ?? new();
      var geoName = DeserializeGroup<GeoNameSettings>(root, "GeoName") ?? new();
      var imagesToVideo = DeserializeGroup<ImagesToVideoSettings>(root, "ImagesToVideo") ?? new();
      var mediaItem = DeserializeGroup<MediaItemSettings>(root, "MediaItem") ?? new();
      var segment = DeserializeGroup<SegmentSettings>(root, "Segment") ?? new();

      var settings = new Settings(common, geoName, imagesToVideo, mediaItem, segment);

      return settings;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return CreateNew();
    }
  }

  private static T? DeserializeGroup<T>(JsonElement root, string name) =>
    root.TryGetProperty(name, out JsonElement elm)
      ? JsonSerializer.Deserialize<T>(elm.GetRawText())
      : default;

  private static Settings CreateNew() =>
    new(new(), new(), new(), new(), new());

  public void OnClosing() {
    if (Modified &&
        Dialog.Show(new MessageDialog(
          "Settings changes",
          "There are some changes in settings. Do you want to save them?",
          MH.UI.Res.IconQuestion,
          true)) == 1)
      Save();
  }
}

public sealed class CommonSettings : ObservableObject {
  private string _cachePath = @":\Temp\PictureManagerCache";
  private string[]? _directorySelectFolders;
  private string _ffmpegPath = string.Empty;
  private int _jpegQuality = 80;
  
  public string CachePath { get => _cachePath; set => OnCachePathChange(value); }
  public string[]? DirectorySelectFolders { get => _directorySelectFolders; set { _directorySelectFolders = value; OnPropertyChanged(); } }
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
  private string _userName = string.Empty;

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

public sealed class SegmentSettings : ObservableObject {
  private int _groupSize = 250;

  public int GroupSize { get => _groupSize; set { _groupSize = value; OnPropertyChanged(); } }
}