using MH.Utils;
using System.Windows.Media.Imaging;

namespace MH.UI.WPF.Extensions;

public static class OrientationExtensions {
  public static Rotation ToRotation(this Orientation orientation) =>
    orientation switch {
      Orientation.Rotate90 => Rotation.Rotate270,
      Orientation.Rotate180 => Rotation.Rotate180,
      Orientation.Rotate270 => Rotation.Rotate90,
      _ => Rotation.Rotate0
    };
}