using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.IO;
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
    SaveCommand = new(Save, () => Modified, null, "Save");
  }

  public static T? DeserializeGroup<T>(JsonElement root, string name) =>
    root.TryGetProperty(name, out JsonElement elm)
      ? JsonSerializer.Deserialize<T>(elm.GetRawText())
      : default;

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

  public virtual void OnClosing() {
    if (Modified &&
        Dialog.Show(new MessageDialog(
          "Settings changes",
          "There are some changes in settings. Do you want to save them?",
          Res.IconQuestion,
          true)) == 1)
      Save();
  }
}