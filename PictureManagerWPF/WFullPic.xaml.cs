using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using PictureManager.Dialogs;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WFullPic.xaml
  /// </summary>
  public partial class WFullPic {
    public AppCore ACore;
    private static System.Timers.Timer _tmrPresentation;

    public WFullPic() {
      InitializeComponent();
      ACore = (AppCore) Application.Current.Properties[nameof(AppProps.AppCore)];
      _tmrPresentation = new System.Timers.Timer(2000);
      _tmrPresentation.Elapsed += delegate {
        if (ACore.MediaItems.CurrentItemMove(true))
          SetCurrentImage();
      };
      WbFullPic.ObjectForScripting = new ScriptManager(ACore);
      WbFullPic.DocumentCompleted += WbFullPicOnDocumentCompleted;

      Stream stream = null;
      try {
        stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PictureManager.html.FullPic.html");
        if (stream != null)
          using (StreamReader reader = new StreamReader(stream)) {
            stream = null;
            WbFullPic.DocumentText = reader.ReadToEnd();
          }
      } finally {
        stream?.Dispose();
      }

      WbFullPic.Focus();
    }

    private void WbFullPicOnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs webBrowserDocumentCompletedEventArgs) {
      if (WbFullPic.Document?.Body == null) return;
      WbFullPic.Document.Body.DoubleClick += WbFullPic_DblClick;
      WbFullPic.Document.Body.KeyDown += WbFullPic_KeyDown;
      SetCurrentImage();
    }

    private void WbFullPic_DblClick(object sender, HtmlElementEventArgs e) {
      SwitchToBrowser();
      Hide();
    }

    public void SetCurrentImage() {
      var current = ACore.MediaItems.Current;
      var filePath = current == null ? string.Empty : current.FilePath;

      var o = 0;
      if (current != null) {
        switch (current.Orientation) {
          case 1: o = 0; break;
          case 3: o = 180; break;
          case 6: o = 90; break;
          case 8: o = 270; break;
        }
      }

      WbFullPic.Document?.InvokeScript("SetPicture", new object[] {filePath, o});
      WbFullPic.Document?.InvokeScript("SetInfo", new object[] {ACore.MediaItems.GetFullScreenInfo()});
    }

    private void SwitchToBrowser() {
      WbFullPic.Document?.GetElementById("fullPicA")?.SetAttribute("src", "");
      WbFullPic.Document?.GetElementById("fullPicB")?.SetAttribute("src", "");
      ACore.WMain.SwitchToBrowser();
    }

    private void WbFullPic_KeyDown(object sender, HtmlElementEventArgs e) {
      if (e.AltKeyPressed && e.KeyPressedCode == 77) {
        DirectorySelectDialog dsd = new DirectorySelectDialog { Owner = this, Title = "Move to" };
        if (dsd.ShowDialog() ?? true) {
          if (ACore.FileOperation(FileOperations.Move, dsd.Answer))
            SetNewMediaItemAfterDeleteOrMove();
        }
        e.ReturnValue = false;
      }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e) {
      switch (e.Key) {
        case Key.Right:
        case Key.PageDown: {
          if (ACore.MediaItems.CurrentItemMove(true))
            SetCurrentImage();
          break;
        }
        case Key.Left:
        case Key.PageUp: {
          if (ACore.MediaItems.CurrentItemMove(false))
            SetCurrentImage();
          break;
        }
        case Key.Escape: {
          if (ACore.ViewerOnly) {
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
          if (ACore.MediaItems.Items.Count(x => x.IsSelected) != 1) return;
          var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
          if (result == MessageBoxResult.Yes)
            if (ACore.FileOperation(FileOperations.Delete, true)) {
              SetNewMediaItemAfterDeleteOrMove();
            }
          break;
        }
        case Key.P: {
          _tmrPresentation.Enabled = !_tmrPresentation.Enabled;
          break;
        }
      }

      if (ACore.KeywordsEditMode) {
        switch (e.Key) {
          case Key.D0:
          case Key.D1:
          case Key.D2:
          case Key.D3:
          case Key.D4:
          case Key.D5: {
            var mi = ACore.MediaItems.Current;
            if (mi != null) {
              mi.IsModifed = true;
              mi.Rating = (int) e.Key - 34;
              mi.WbUpdateInfo();
              WbFullPic.Document?.InvokeScript("SetInfo", new object[] {ACore.MediaItems.GetFullScreenInfo()});
            }
            break;
          }
        }
      }
    }

    private void SetNewMediaItemAfterDeleteOrMove() {
      var index = ACore.MediaItems.Current.Index;
      ACore.MediaItems.RemoveSelectedFromWeb();
      var itemsCount = ACore.MediaItems.Items.Count;
      if (itemsCount > index)
        ACore.MediaItems.Items[index].IsSelected = true;
      if (itemsCount <= index && itemsCount != 0)
        ACore.MediaItems.Items[index - 1].IsSelected = true;
      ACore.MediaItems.SetCurrent();
      SetCurrentImage();
    }

    private void MetroWindow_Activated(object sender, System.EventArgs e) {
      Topmost = true;
    }

    private void MetroWindow_Deactivated(object sender, System.EventArgs e) {
      Topmost = false;
    }
  }
}
