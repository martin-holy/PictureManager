using System.Windows.Forms;

namespace PictureManager.ViewModel {
  public class Picture: BaseMediaItem {
    public Picture(string filePath, DataModel.PmDataContext db, int index, WebBrowser wbThumbs, DataModel.MediaItem data)
      : base(filePath, db, index, wbThumbs, data) {}
  }
}
