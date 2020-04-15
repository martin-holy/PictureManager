using System.Windows;

namespace PictureManager.Dialogs {
  public partial class SettingsDialog {
    public SettingsDialog() {
      InitializeComponent();
    }

    private void Save(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }
  }
}
