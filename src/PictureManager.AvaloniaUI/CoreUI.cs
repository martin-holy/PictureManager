using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using AUI = MH.UI.AvaloniaUI;

namespace PictureManager.AvaloniaUI;

public class CoreUI : ICoreP {
  public CoreUI() {
    AUI.Utils.Init.SetDelegates();
    AUI.Resources.Dictionaries.IconToBrush = Res.IconToBrushDic;

    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;
  }

  public void AfterInit() {
    // TODO PORT
  }

  public void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) =>
    Utils.Imaging.CreateImageThumbnail(srcPath, destPath, desiredSize, quality);
}