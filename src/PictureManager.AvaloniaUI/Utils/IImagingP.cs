using Avalonia.Media.Imaging;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.AvaloniaUI.Utils;

public interface IImagingP {
  public void CreateImageThumbnail(MediaItemM mi);
  public Bitmap? GetImageThumbnail(MediaItemM mi);
}