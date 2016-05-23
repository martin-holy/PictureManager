using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace PictureManager {
  [ComVisible(true)]
  public class ScriptManager {
    public AppCore ACore;

    public ScriptManager(AppCore appCore) {
      ACore = appCore;
    }

    public void FullPicMouseWheel(int delta) {
      ACore.MediaItems.CurrentItemMove(delta < 0);
      ACore.WMain.ShowFullPicture();
    }

    public void OnContextMenu() {
      ACore.WMain.WbThumbsShowContextMenu();
    }

    public void Thumbs_OnDragStart() {
      var dob = new DataObject();
      dob.SetData(DataFormats.FileDrop, ACore.MediaItems.Items.Where(x => x.IsSelected).Select(p => p.FilePath).ToArray());
      DragDrop.DoDragDrop(ACore.WMain, dob, DragDropEffects.Move | DragDropEffects.Copy);
    }
  }
}