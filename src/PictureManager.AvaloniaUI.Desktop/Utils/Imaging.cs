using System.IO;
using Avalonia.Media.Imaging;
using PictureManager.AvaloniaUI.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.AvaloniaUI.Desktop.Utils;

public class Imaging : IImagingP {
  public void CreateImageThumbnail(MediaItemM mi) =>
    AvaloniaUI.Utils.Imaging.CreateImageThumbnail(
      mi.FilePath,
      mi.FilePathCache,
      Core.Settings.MediaItem.ThumbSize,
      Core.Settings.Common.JpegQuality);

  public Bitmap? GetImageThumbnail(MediaItemM mi) {
    if (!File.Exists(mi.FilePathCache)) return null;
    return new(mi.FilePathCache);
  }
}