using PictureManager.Common;
using PictureManager.Common.Features.Folder;

namespace PictureManager.MAUI;

public class CoreUI: ICoreP {
  public CoreUI() {
    // TODO PORT
  }

  public void AfterInit() {
    Core.Settings.MediaItem.MediaItemThumbScale = 0.2;
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) => throw new System.NotImplementedException();
  public string GetFilePathCache(FolderM folder, string fileNameCache) => throw new System.NotImplementedException();
  public string GetFolderPathCache(FolderM folder) => throw new System.NotImplementedException();
}