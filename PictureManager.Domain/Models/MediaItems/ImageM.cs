namespace PictureManager.Domain.Models.MediaItems;

public sealed class ImageM : RealMediaItemM {
  public ImageM(int id, FolderM folder, string fileName) : base(id, folder, fileName) { }
}