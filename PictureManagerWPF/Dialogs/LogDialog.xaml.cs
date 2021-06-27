using System.Windows;

namespace PictureManager.Dialogs {
  public partial class LogDialog {
    public LogDialog() {
      InitializeComponent();
    }

    private void BtnClear_OnClick(object sender, RoutedEventArgs e) {
      App.Ui.Log.Clear();
      Close();
    }
  }
}
