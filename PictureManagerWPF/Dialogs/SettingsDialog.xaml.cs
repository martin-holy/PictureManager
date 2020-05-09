using System.Windows;
using PictureManager.Properties;

namespace PictureManager.Dialogs {
  public partial class SettingsDialog {
    public SettingsDialog() {
      InitializeComponent();
    }

    private void Save(object sender, RoutedEventArgs e) {
      App.Core.Model.CachePath = Settings.Default.CachePath;
      App.Core.Model.ThumbnailSize = Settings.Default.ThumbnailSize;

      DialogResult = true;
    }
  }
}
