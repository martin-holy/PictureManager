﻿namespace PictureManager.Properties {


  // This class allows you to handle specific events on the settings class:
  //  The SettingChanging event is raised before a setting's value is changed.
  //  The PropertyChanged event is raised after a setting's value is changed.
  //  The SettingsLoaded event is raised after the setting values are loaded.
  //  The SettingsSaving event is raised before the setting values are saved.
  internal sealed partial class Settings {

    public Settings() {
      // // To add event handlers for saving and changing settings, uncomment the lines below:
      //
      this.SettingChanging += this.SettingChangingEventHandler;
      //
      // this.SettingsSaving += this.SettingsSavingEventHandler;
      //
    }

    private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
      switch (e.SettingName) {
        case "CachePath": {
          var val = (string) e.NewValue;
          if (val.Length < 4 || !val.StartsWith(":\\") || !val.EndsWith("\\")) {
            e.Cancel = true;
          }
          break;
        }
      }
    }

    private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
      // Add code to handle the SettingsSaving event here.
    }
  }
}
