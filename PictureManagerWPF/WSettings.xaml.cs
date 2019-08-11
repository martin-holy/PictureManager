using System.Windows;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WSettings.xaml
  /// </summary>
  public partial class WSettings {
    public WSettings() {
      InitializeComponent();
    }

    private void Save(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }
  }
}
