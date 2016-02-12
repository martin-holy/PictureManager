using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

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
      WbFullPic.DocumentCompleted += WbFullPicOnDocumentCompleted;

      using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PictureManager.html.FullPic.html"))
        if (stream != null)
          using (StreamReader reader = new StreamReader(stream)) {
            WbFullPic.DocumentText = reader.ReadToEnd();
          }

      WbFullPic.Focus();
    }

    private void WbFullPicOnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs webBrowserDocumentCompletedEventArgs) {
      if (WbFullPic.Document?.Body == null) return;
      WbFullPic.Document.Body.DoubleClick += WbFullPic_DblClick;
      SetCurrentImage();
    }

    private void WbFullPic_DblClick(object sender, HtmlElementEventArgs e) {
      SwitchToBrowser();
      Hide();
    }

    public void SetCurrentImage() {
      if (_wMain.ACore.CurrentPicture == null) return;
      WbFullPic.Document?.GetElementById("fullPic")?.SetAttribute("src", _wMain.ACore.CurrentPicture.FilePath);
    }

    private void SwitchToBrowser() {
      WbFullPic.Document?.GetElementById("fullPic")?.SetAttribute("src", "");
      _wMain.SwitchToBrowser();
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
              SwitchToBrowser();
              Hide();
            }
            break;
          }
        case Key.Enter:
          {
            SwitchToBrowser();
            Hide();
            break;
          }
      }
    }
  }
}
