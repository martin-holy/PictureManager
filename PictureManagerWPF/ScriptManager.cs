using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

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

    public void Thumbs_OnDragStart() {
      var dob = new DataObject();
      dob.SetData(DataFormats.FileDrop, _wMain.ACore.SelectedPictures.Select(p => p.FilePath).ToArray());
      DragDrop.DoDragDrop(_wMain, dob, DragDropEffects.Move | DragDropEffects.Copy);
    }
  }
}