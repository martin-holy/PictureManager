namespace PictureManager.Properties {
  // This class allows you to handle specific events on the settings class:
  //  The SettingChanging event is raised before a setting's value is changed.
  //  The PropertyChanged event is raised after a setting's value is changed.
  //  The SettingsLoaded event is raised after the setting values are loaded.
  //  The SettingsSaving event is raised before the setting values are saved.
  internal sealed partial class Settings {

    public Settings() {
      SettingChanging += SettingChangingEventHandler;
    }

    private static void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
      switch (e.SettingName) {
        case "CachePath": {
          var val = (string)e.NewValue;
          if (val.Length < 4 || !val.StartsWith(":\\") || !val.EndsWith("\\")) {
            e.Cancel = true;
          }
          break;
        }
      }
    }
  }
}
