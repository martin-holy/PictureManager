using Android.App;
using PictureManager.Common;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android;

public class CoreUI: ICoreP {
  public CoreUI() {
    // TODO PORT
    MH.UI.Android.Utils.Init.SetDelegates();
    MH.UI.Android.Utils.Icons.IconNameToColor = Resources.Res.IconToColorDic;
    MH.UI.Android.Utils.DisplayU.Init(Application.Context!.Resources!.DisplayMetrics!);
    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
  }

  public void AfterInit() {
    Core.Settings.MediaItem.MediaItemThumbScale = 0.5;
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) => throw new System.NotImplementedException();
  public string GetFilePathCache(FolderM folder, string fileNameCache) => throw new System.NotImplementedException();
  public string GetFolderPathCache(FolderM folder) => throw new System.NotImplementedException();
}