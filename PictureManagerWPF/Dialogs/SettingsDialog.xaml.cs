using System.Windows;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for WSettings.xaml
  /// </summary>
  public partial class SettingsDialog {
    public SettingsDialog() {
      InitializeComponent();
    }

    private void Save(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }
  }
}
