namespace MH.Utils.Extensions;

public static class OrientationExtensions {
  public static int ToAngle(this Orientation orientation) =>
    orientation switch {
      Orientation.Rotate90 => 90,
      Orientation.Rotate180 => 180,
      Orientation.Rotate270 => 270,
      _ => 0,
    };

  public static Orientation SwapRotateIf(this Orientation orientation, bool swap) =>
    swap
      ? orientation switch {
        Orientation.Rotate270 => Orientation.Rotate90,
        Orientation.Rotate90 => Orientation.Rotate270,
        _ => orientation
      }
      : orientation;

  public static int ToInt(this Orientation orientation) =>
    (int)orientation;
}