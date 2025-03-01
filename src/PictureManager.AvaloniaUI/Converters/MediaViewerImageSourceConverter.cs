using Avalonia.Media.Imaging;
using MH.UI.AvaloniaUI.Converters;
using MH.Utils;
using PictureManager.Common.Features.MediaItem.Image;
using System;

namespace PictureManager.AvaloniaUI.Converters;

public class MediaViewerImageSourceConverter : BaseConverter {
  private static readonly object _lock = new();
  private static MediaViewerImageSourceConverter? _inst;
  public static MediaViewerImageSourceConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  // TODO PORT orientation
  public override object? Convert(object? value, object? parameter) {
    try {
      return value is ImageM mi
        ? new Bitmap(mi.FilePath)
        : null;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}