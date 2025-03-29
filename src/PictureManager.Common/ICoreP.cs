using PictureManager.Common.Features.Folder;

namespace PictureManager.Common;

public interface ICoreP {
  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality);
  public string GetFilePathCache(FolderM folder, string fileNameCache);
  public string GetFolderPathCache(FolderM folder);
}