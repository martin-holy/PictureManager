namespace PictureManager.Common;

public interface ICoreP {
  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality);
}