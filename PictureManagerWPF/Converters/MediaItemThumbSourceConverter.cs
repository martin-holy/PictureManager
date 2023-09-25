using MH.UI.WPF.Converters;
using MH.Utils;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PictureManager.Converters; 

public class MediaItemThumbSourceConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) {
    try {
      if (value is not MediaItemM mi)
        return Binding.DoNothing;

      // TODO use buffer system like in segment
      if (!File.Exists(mi.FilePathCache)) {
        if (!File.Exists(mi.FilePath)) {
          Core.MediaItemsM.Delete(new List<MediaItemM> { mi });
          return Binding.DoNothing;
        }

        Imaging.CreateThumbnailAsync(
          mi.MediaType,
          mi.FilePath,
          mi.FilePathCache,
          Core.Settings.ThumbnailSize,
          0,
          Core.Settings.JpegQualityLevel).GetAwaiter().GetResult();
      }

      var orientation = mi.Orientation;
      // swap 90 and 270 degrees for video
      if (mi.MediaType == MediaType.Video) {
        if (mi.Orientation == 6)
          orientation = 8;
        else if (mi.Orientation == 8)
          orientation = 6;
      }

      var src = new BitmapImage();
      src.BeginInit();
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.UriSource = new(mi.FilePathCache);
      src.Rotation = Imaging.MediaOrientation2Rotation((MediaOrientation)orientation);

      if (MediaItemsM.ThumbIgnoreCache.Remove(mi))
        src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

      src.EndInit();

      return src;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return Binding.DoNothing;
    }
  }
}