using System.Windows.Forms;

namespace PictureManager.Data {
  public class Picture: BaseMediaItem {
    public Picture(string filePath, DbStuff db, int index, WebBrowser wbThumbs) : base(filePath, db, index, wbThumbs) {}
  }
}
