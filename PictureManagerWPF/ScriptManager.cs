using System.Runtime.InteropServices;

namespace PictureManager {
  [ComVisible(true)]
  public class ScriptManager {
    readonly WMain _wMain;

    public ScriptManager(WMain wMain) {
      _wMain = wMain;
    }

    public void FullPicMouseWheel(int delta) {
      _wMain.ACore.CurrentPictureMove(delta < 0);
      _wMain.ShowFullPicture();
    }

    public void OnContextMenu() {
      _wMain.WbThumbsShowContextMenu();
    }
  }
}