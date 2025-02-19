using PictureManager.Common.Features.MediaItem;

namespace PictureManager.AvaloniaUI.ViewModels;

public static class MediaItemVM {
  public static void ReadMetadata(MediaItemMetadata mim, bool gpsOnly = false) {
    // TODO PORT
    mim.Width = 100;
    mim.Height = 100;
    mim.Success = true;
  }
}