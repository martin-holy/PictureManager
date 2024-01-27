using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models.MediaItems;
using System.Linq;

namespace PictureManager.Domain.Dialogs;

public sealed class RotationDialogM : Dialog {
  private static RotationDialogM _inst;

  public static RelayCommand<object> OpenCommand { get; } =
    new(Open, () => Core.MediaItemsM.GetActive().OfType<RealMediaItemM>().Any());
  public RelayCommand<Orientation> RotationCommand { get; }

  public RotationDialogM() : base("Rotation", Res.IconImageMultiple) {
    RotationCommand = new(mo => Result = (int)mo);
  }

  private static void Open() {
    _inst ??= new();
    var rotation = Show(_inst);
    if (rotation == 0) return;
    Core.Db.MediaItems.SetOrientation(
      Core.MediaItemsM.GetActive().OfType<RealMediaItemM>().ToArray(),
      (Orientation)rotation,
      SetOrientation);
  }

  private static void SetOrientation(RealMediaItemM mi, Orientation orientation) {
    var newOrientation = mi.Orientation.ToAngle();

    if (mi is ImageM)
      switch (orientation) {
        case Orientation.Rotate90: newOrientation += 90; break;
        case Orientation.Rotate180: newOrientation += 180; break;
        case Orientation.Rotate270: newOrientation += 270; break;
      }
    else if (mi is VideoM) // images have switched 90 and 270 angles and all application is made with this in mind
      // so I switched orientation just for video
      switch (orientation) {
        case Orientation.Rotate90: newOrientation += 270; break;
        case Orientation.Rotate180: newOrientation += 180; break;
        case Orientation.Rotate270: newOrientation += 90; break;
      }

    if (newOrientation >= 360) newOrientation -= 360;

    switch (newOrientation) {
      case 0: mi.Orientation = Orientation.Normal; break;
      case 90: mi.Orientation = Orientation.Rotate90; break;
      case 180: mi.Orientation = Orientation.Rotate180; break;
      case 270: mi.Orientation = Orientation.Rotate270; break;
    }
  }
}