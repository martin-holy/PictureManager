using System.Windows;

namespace MH.UI.WPF.Extensions;

public static class Int32RectExtensions {
  public static Int32Rect Scale(this Int32Rect rect, double scale) =>
    new((int)(rect.X * scale), (int)(rect.Y * scale), (int)(rect.Width * scale), (int)(rect.Height * scale));
}