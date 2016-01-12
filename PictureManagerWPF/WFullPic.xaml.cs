using System.Windows;
using System.Windows.Input;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WFullPic.xaml
  /// </summary>
  public partial class WFullPic {
    readonly WMain _wMain;

    public WFullPic(WMain wMain) {
      InitializeComponent();
      _wMain = wMain;
      WbFullPic.ObjectForScripting = new ScriptManager(_wMain);
      WbFullPic.Navigate(_wMain.WbFullPicHtmlPath);
      WbFullPic.Focus();
    }

    public void SetCurrentImage() {
      if (WbFullPic.IsLoaded)
        WbFullPic.InvokeScript("setAttrOfElementById", "fullPic", "src", _wMain.ACore.CurrentPicture.FilePath);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e) {
      switch (e.Key) {
        case Key.Right:
        case Key.PageDown:
          {
            if (_wMain.ACore.CurrentPictureMove(true))
              SetCurrentImage();
            break;
          }
        case Key.Left:
        case Key.PageUp:
          {
            if (_wMain.ACore.CurrentPictureMove(false))
              SetCurrentImage();
            break;
          }
        case Key.Escape:
          {
            if (_wMain.ACore.ViewerOnly) {
              Application.Current.Shutdown();
            } else {
              _wMain.SwitchToBrowser();
              Hide();
            }
            break;
          }
        case Key.Enter:
          {
            _wMain.SwitchToBrowser();
            Hide();
            break;
          }
      }
    }

    private void WbFullPic_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e) {
      SetCurrentImage();
    }
  }
}
