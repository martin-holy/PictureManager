using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MH.UI.BaseClasses;

public abstract class UserSettings {
  private readonly string _filePath;

  [JsonIgnore]
  public bool Modified { get; set; }
  [JsonIgnore]
  public ListItem[] Groups { get; protected set; } = null!;
  [JsonIgnore]
  public RelayCommand SaveCommand { get; }

  protected UserSettings(string filePath) {
    _filePath = filePath;
    SaveCommand = new(Save, () => Modified, Res.IconSave, "Save");
  }

  protected void WatchForChanges() {
    foreach (var item in Groups.Select(x => x.Data).OfType<ObservableObject>())
      item.PropertyChanged += delegate { Modified = true; };
  }

  public static T? DeserializeGroup<T>(JsonElement root, string name) =>
    root.TryGetProperty(name, out JsonElement elm)
      ? JsonSerializer.Deserialize<T>(elm.GetRawText())
      : default;

  public void Save() {
    try {
      var opt = new JsonSerializerOptions { WriteIndented = true };
      File.WriteAllText(_filePath, Serialize(opt));
      Modified = false;
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  protected abstract string Serialize(JsonSerializerOptions options);
}