namespace MH.Utils.Extensions;

public static class OrientationExtensions {
  public static int ToAngle(this Orientation orientation) =>
    orientation switch {
      Orientation.Rotate90 => 90,
      Orientation.Rotate180 => 180,
      Orientation.Rotate270 => 270,
      _ => 0,
    };

  public static Orientation Rotate(this Orientation orientation, Orientation rotation) {
    var angle = orientation.ToAngle() + rotation.ToAngle();
    if (angle >= 360) angle -= 360;

    return angle switch {
      0 => Orientation.Normal,
      90 => Orientation.Rotate90,
      180 => Orientation.Rotate180,
      270 => Orientation.Rotate270,
      _ => Orientation.Normal
    };
  }

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