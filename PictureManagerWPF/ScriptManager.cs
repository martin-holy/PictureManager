using System.Runtime.InteropServices;

namespace PictureManager {
  [ComVisible(true)]
  public class ScriptManager {
    readonly WMain _wMain;

    public ScriptManager(WMain wMain) {
      _wMain = wMain;
    }

    public void Test() {

    }

    public void ShowFullPicture(int index) {
      _wMain.ACore.CurrentPicture = _wMain.ACore.Pictures[index];
      _wMain.ShowFullPicture();
    }

    public void FullPicMouseWheel(int delta) {
      _wMain.ACore.CurrentPictureMove(delta < 0);
      _wMain.ShowFullPicture();
    }

    public void SwitchToBrowser() {
      _wMain.SwitchToBrowser();
    }

    public void OnContextMenu() {
      _wMain.WbThumbsShowContextMenu();
    }

    public void SetSelected(string ids) {
      _wMain.ACore.SetSelectedPictures(ids);
    }
  }
}