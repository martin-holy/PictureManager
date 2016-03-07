using System.Windows;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WSettings.xaml
  /// </summary>
  public partial class WSettings {
    public WSettings() {
      InitializeComponent();
      DirListFkIgnoreList.SettingsPropertyName = "FolderKeywordIngnoreList";
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }
  }
}
