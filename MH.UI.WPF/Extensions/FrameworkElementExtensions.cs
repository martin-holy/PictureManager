using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MH.UI.WPF.Extensions;

public static class FrameworkElementExtensions {
  public static CroppedBitmap ToBitmap(this FrameworkElement fe) {
    var offset = fe.TranslatePoint(new(0, 0), (UIElement)fe.Parent);
    var ox = (int)Math.Round(offset.X, 0);
    var oy = (int)Math.Round(offset.Y, 0);
    var aw = (int)Math.Round(fe.ActualWidth, 0);
    var ah = (int)Math.Round(fe.ActualHeight, 0);

    var bmp = new RenderTargetBitmap(aw + ox, ah + oy, 96, 96, PixelFormats.Pbgra32);
    bmp.Render(fe);

    return new(bmp, new(ox, oy, aw, ah));
  }
}