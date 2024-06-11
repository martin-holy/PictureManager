using System.Windows;
using System.Windows.Media;

namespace MH.UI.WPF.Utils;

public static class ColorHelper {
  public static void AddGradientColors(string name, Color color, int samples = 9) {
    Application.Current.Resources[name] = color;

    samples++; // to skip color without transparency
    var size = 255.0 / samples;
    for (int i = 1; i < samples; i++)
      Application.Current.Resources[$"{name}{i}"] = Color.FromArgb((byte)(size * i), color.R, color.G, color.B);
  }

  public static void AddColorsToResources() {
    AddGradientColors("MH.Color.Accent", SystemParameters.WindowGlassColor);
    AddGradientColors("MH.Color.Black", Color.FromRgb(0, 0, 0));
    AddGradientColors("MH.Color.White", Color.FromRgb(255, 255, 255));
  }
}