using System.Windows;
using System.Windows.Media;

namespace MH.UI.WPF.Utils;

public static class ColorHelper {
  public static void AddGradients(string name, Color color, int samples = 9) {
    var colorName = $"MH.Color.{name}";
    var brushName = $"MH.B.{name}";

    Application.Current.Resources[colorName] = color;
    Application.Current.Resources[brushName] = CreateBrush(color);

    samples++; // to skip color without transparency
    var size = 255.0 / samples;
    for (int i = 1; i < samples; i++) {
      var gColor = Color.FromArgb((byte)(size * i), color.R, color.G, color.B);
      Application.Current.Resources[$"{colorName}{i}"] = gColor;
      Application.Current.Resources[$"{brushName}{i}"] = CreateBrush(gColor);
    }
  }

  public static void AddVariants(string name, Color color) {
    var colorName = $"MH.Color.{name}";
    var brushName = $"MH.B.{name}";

    MH.Utils.Imaging.RgbToHsl(color.R, color.G, color.B, out var h, out var s, out var l);
    l = 50;
    MH.Utils.Imaging.HslToRgb(h, s, l, out var r, out var g, out var b);
    var darkColor = Color.FromRgb(r, g, b);
    Application.Current.Resources[$"{colorName}-Dark"] = darkColor;
    Application.Current.Resources[$"{brushName}-Dark"] = CreateBrush(darkColor);
  }

  private static Brush CreateBrush(Color color) {
    var brush = new SolidColorBrush(color);
    brush.Freeze();
    return brush;
  }

  public static void AddColorsToResources() {
    AddVariants("Accent", SystemParameters.WindowGlassColor);
    AddGradients("Accent", SystemParameters.WindowGlassColor);
    AddGradients("Black", Color.FromRgb(0, 0, 0));
    AddGradients("White", Color.FromRgb(255, 255, 255));
  }
}