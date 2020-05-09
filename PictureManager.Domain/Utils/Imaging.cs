using System;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Utils {
  public static class Imaging {
    public static string[] SupportedExts = { ".jpg", ".jpeg", ".mp4", ".mkv" };
    public static string[] SupportedImageExts = { ".jpg", ".jpeg" };
    public static string[] SupportedVideoExts = { ".mp4", ".mkv" };

    public static bool IsSupportedFileType(string filePath) {
      return SupportedExts.Any(x => x.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase));
    }

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
          return;
        }

        outHeight = (int)(desiredSize / width * height);
        outWidth = desiredSize;
        return;
      }

      outHeight = desiredSize;
      outWidth = (int)(desiredSize / height * width);
    }
  }
}
