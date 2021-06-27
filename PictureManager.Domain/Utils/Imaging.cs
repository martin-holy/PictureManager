using System;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Utils {
  public static class Imaging {
    private static readonly string[] SupportedExts = { ".jpg", ".jpeg", ".mp4" };
    private static readonly string[] SupportedImageExts = { ".jpg", ".jpeg" };

    public static bool IsSupportedFileType(string filePath) =>
      SupportedExts.Any(x => x.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase));

    public static MediaType GetMediaType(string filePath) {
      return SupportedImageExts.Any(
        x => filePath.EndsWith(x, StringComparison.InvariantCultureIgnoreCase))
        ? MediaType.Image
        : MediaType.Video;
    }

    public static void GetThumbSize(double width, double height, int desiredSize, out int outWidth, out int outHeight) {
      if (width > height) {
        //panorama
        if (width / height > 16.0 / 9.0) {
          const int maxWidth = 1100;
          var panoramaHeight = desiredSize / 16.0 * 9;
          var tooBig = panoramaHeight / height * width > maxWidth;
          outHeight = (int)(tooBig ? maxWidth / width * height : panoramaHeight);
          outWidth = (int)(tooBig ? maxWidth : panoramaHeight / height * width);
          if (outHeight % 2 != 0) outHeight++;
          if (outWidth % 2 != 0) outWidth++;
          return;
        }

        outHeight = (int)(desiredSize / width * height);
        outWidth = desiredSize;
        if (outHeight % 2 != 0) outHeight++;
        return;
      }

      outHeight = desiredSize;
      outWidth = (int)(desiredSize / height * width);
      if (outWidth % 2 != 0) outWidth++;
    }
  }
}
