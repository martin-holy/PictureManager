using MH.Utils;
using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Dialogs;

public sealed class RotationDialogM : Dialog {
  private static RotationDialogM _inst;

  public RelayCommand<Orientation> RotationCommand { get; }

  public RotationDialogM() : base("Rotation", Res.IconImageMultiple) {
    RotationCommand = new(o => Result = (int)o);
  }

  public static bool Open(out Orientation rotation) {
    _inst ??= new();
    var result = Show(_inst);
    rotation = result == 0 ? Orientation.Normal : (Orientation)result;
    return result != 0;
  }
}