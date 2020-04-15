using System.Windows;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for LogDialog.xaml
  /// </summary>
  public partial class LogDialog {
    public LogDialog() {
      InitializeComponent();
    }

    private void BtnClear_OnClick(object sender, RoutedEventArgs e) {
      App.Core.Log.Clear();
      Close();
    }
  }
}
