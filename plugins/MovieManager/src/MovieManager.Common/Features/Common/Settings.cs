using MH.UI.BaseClasses;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common;
using System;
using System.IO;
using System.Text.Json;

namespace MovieManager.Common.Features.Common;

public sealed class Settings : UserSettings {
  public CommonSettings Common { get; }
  public ImportSettings Import { get; }

  public Settings(string filePath, CommonSettings common, ImportSettings import) : base(filePath) {
    Common = common;
    Import = import;

    Groups = [
      new(Res.IconSettings, "Common", common),
      new(Res.IconImport, "Import", import)
    ];

    WatchForChanges();
  }

  public static Settings Load(string filePath) {
    if (!File.Exists(filePath)) return CreateNew(filePath);
    try {
      using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
      var root = doc.RootElement;

      var common = DeserializeGroup<CommonSettings>(root, "Common") ?? new();
      var import = DeserializeGroup<ImportSettings>(root, "Import") ?? new();

      var settings = new Settings(filePath, common, import);

      return settings;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return CreateNew(filePath);
    }
  }

  private static Settings CreateNew(string filePath) =>
    new(filePath, new(), new());

  protected override string Serialize(JsonSerializerOptions options) =>
    JsonSerializer.Serialize(this, options);
}

public sealed class CommonSettings : ObservableObject {
  private bool _showToolBar;
  
  public bool ShowToolBar { get => _showToolBar; set { _showToolBar = value; OnPropertyChanged(); } }
}

public sealed class ImportSettings : ObservableObject {
  private int _maxImageSize = 12;
  
  public int MaxImageSize { get => _maxImageSize; set { _maxImageSize = value; OnPropertyChanged(); } }
}