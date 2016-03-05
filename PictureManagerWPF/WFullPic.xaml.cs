using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

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
      var current = _wMain.ACore.MediaItems.Current;
      var filePath = current == null ? string.Empty : current.FilePath;
      var fullPic = WbFullPic.Document?.GetElementById("fullPic");
      if (fullPic == null) return;

      /*var newFullPic = WbFullPic.Document.CreateElement("img");
      if (newFullPic == null) return;
      newFullPic.OuterHtml = fullPic.OuterHtml.Replace("id=\"fullPic\"", "id=\"newFullPic\"");
      newFullPic.SetAttribute("src", filePath);
      newFullPic.Style = string.Empty;
      if (current != null) {
        var o = 0;
        switch (current.Orientation) {
          case 1: o = 0; break;
          case 3: o = 180; break;
          case 6: o = 90; break;
          case 8: o = 270; break;
        }
        newFullPic.Style = $"transform: rotate({o}deg)";
      }

      fullPic.OuterHtml = newFullPic.OuterHtml.Replace("id=\"newFullPic\"", "id=\"fullPic\"");*/


      //TODO: BeginUpdate and EndUpdate na fullPic, nebo to udelat vsechno pres style
      fullPic.SetAttribute("src", filePath);
      fullPic.Style = string.Empty;
      if (current != null) {
        var o = 0;
        switch (current.Orientation) {
          case 1: o = 0; break;
          case 3: o = 180; break;
          case 6: o = 90; break;
          case 8: o = 270; break;
        }
        fullPic.Style = $"transform: rotate({o}deg)";
      }
    }

    private void SwitchToBrowser() {
      WbFullPic.Document?.GetElementById("fullPic")?.SetAttribute("src", "");
      _wMain.SwitchToBrowser();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e) {
      switch (e.Key) {
        case Key.Right:
        case Key.PageDown: {
            if (_wMain.ACore.MediaItems.CurrentItemMove(true))
              SetCurrentImage();
            break;
          }
        case Key.Left:
        case Key.PageUp: {
            if (_wMain.ACore.MediaItems.CurrentItemMove(false))
              SetCurrentImage();
            break;
          }
        case Key.Escape: {
            if (_wMain.ACore.ViewerOnly) {
              Application.Current.Shutdown();
            } else {
              SwitchToBrowser();
              Hide();
            }
            break;
          }
        case Key.Enter: {
            SwitchToBrowser();
            Hide();
            break;
          }
        case Key.Delete: {
          if (_wMain.ACore.MediaItems.Items.Count(x => x.IsSelected) != 1) return;
          var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
          if (result == MessageBoxResult.Yes)
            if (_wMain.ACore.FileOperation(AppCore.FileOperations.Delete, true)) {
              var index = _wMain.ACore.MediaItems.Current.Index;
              _wMain.ACore.MediaItems.RemoveSelectedFromWeb();
              var itemsCount = _wMain.ACore.MediaItems.Items.Count;
              if (itemsCount > index)
                _wMain.ACore.MediaItems.Items[index].IsSelected = true;
              if (itemsCount <= index && itemsCount != 0)
                _wMain.ACore.MediaItems.Items[index - 1].IsSelected = true;
              _wMain.ACore.MediaItems.SetCurrent();
              SetCurrentImage();
            }
          break;
        }
      }
    }
  }
}
