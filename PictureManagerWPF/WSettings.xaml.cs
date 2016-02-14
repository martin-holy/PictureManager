using System;
using System.Windows;
using PictureManager.Properties;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WSettings.xaml
  /// </summary>
  public partial class WSettings {
    public WSettings() {
      InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e) {
      var paths = Settings.Default.FolderKeywordIngnoreList.Split(new[] {Environment.NewLine},
        StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < paths.Length; i++) {
        if (!paths[i].EndsWith("\\")) paths[i] = paths[i] + "\\";
      }
      Settings.Default.FolderKeywordIngnoreList = string.Join(Environment.NewLine, paths);

      DialogResult = true;
    }
  }
}
