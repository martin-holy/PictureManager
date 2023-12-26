using MH.UI.WPF.Converters;
using MH.Utils;
using PictureManager.Domain;
using PictureManager.Domain.Models.MediaItems;
using System;

namespace PictureManager.Converters;

public class MediaViewerImageSourceConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) {
    try {
      return value is ImageM mi
        ? Utils.Imaging.GetBitmapImage(mi.FilePath, (MediaOrientation)mi.Orientation)
        : null;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}