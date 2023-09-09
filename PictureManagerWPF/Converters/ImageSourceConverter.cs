using MH.UI.WPF.Converters;
using MH.Utils;
using System;
using System.IO;
using System.Net.Cache;
using System.Windows.Media.Imaging;

namespace PictureManager.Converters;

public class ImageSourceConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) {
    try {
      if (value is not string filePath || !File.Exists(filePath)) return null;

      var src = new BitmapImage();
      src.BeginInit();

      if ("IgnoreImageCache".Equals(parameter as string, StringComparison.Ordinal)) {
        src.UriCachePolicy = new(RequestCacheLevel.BypassCache);
        src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
      }

      src.CacheOption = BitmapCacheOption.OnLoad;
      src.UriSource = new(filePath);
      src.EndInit();
      return src;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}