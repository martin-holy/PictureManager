using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;

namespace PictureManager.Common.Dialogs;

public sealed class RotationDialogM : Dialog {
  private static RotationDialogM _inst;

  public RelayCommand Rotate90Command { get; }
  public RelayCommand Rotate180Command { get; }
  public RelayCommand Rotate270Command { get; }

  public RotationDialogM() : base("Rotation", Res.IconImageMultiple) {
    Rotate90Command = new(() => Result = (int)Orientation.Rotate90, Res.IconRotateLeft);
    Rotate180Command = new(() => Result = (int)Orientation.Rotate180, Res.IconRotateClockwise);
    Rotate270Command = new(() => Result = (int)Orientation.Rotate270, Res.IconRotateRight);
  }

  public static bool Open(out Orientation rotation) {
    _inst ??= new();
    var result = Show(_inst);
    rotation = result == 0 ? Orientation.Normal : (Orientation)result;
    return result != 0;
  }
}