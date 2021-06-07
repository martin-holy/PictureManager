using System.Windows;
using PictureManager.Properties;

namespace PictureManager.Dialogs {
  public partial class SettingsDialog {
    public SettingsDialog() {
      InitializeComponent();
    }

    private void Save(object sender, RoutedEventArgs e) {
      App.Core.CachePath = Settings.Default.CachePath;
      App.Core.ThumbnailSize = Settings.Default.ThumbnailSize;

      DialogResult = true;
    }
  }
}
