using MH.Utils.BaseClasses;
using PictureManager.Domain.Models.MediaItems;
using System.Linq;

namespace PictureManager.Domain.Dialogs;

public sealed class RotationDialogM : Dialog {
  private static RotationDialogM _inst;

  public static RelayCommand<object> OpenCommand { get; } =
    new(Open, () => Core.MediaItemsM.GetActive().OfType<RealMediaItemM>().Any());
  public RelayCommand<MediaOrientation> RotationCommand { get; }

  public RotationDialogM() : base("Rotation", Res.IconImageMultiple) {
    RotationCommand = new(mo => Result = (int)mo);
  }

  private static void Open() {
    _inst ??= new();
    var rotation = Show(_inst);
    if (rotation == 0) return;
    Core.Db.MediaItems.SetOrientation(
      Core.MediaItemsM.GetActive().OfType<RealMediaItemM>().ToArray(),
      (MediaOrientation)rotation,
      SetOrientation);
  }

  private static void SetOrientation(RealMediaItemM mi, MediaOrientation orientation) {
    var newOrientation = mi.RotationAngle;

    if (mi is ImageM)
      switch (orientation) {
        case MediaOrientation.Rotate90: newOrientation += 90; break;
        case MediaOrientation.Rotate180: newOrientation += 180; break;
        case MediaOrientation.Rotate270: newOrientation += 270; break;
      }
    else if (mi is VideoM) // images have switched 90 and 270 angles and all application is made with this in mind
      // so I switched orientation just for video
      switch (orientation) {
        case MediaOrientation.Rotate90: newOrientation += 270; break;
        case MediaOrientation.Rotate180: newOrientation += 180; break;
        case MediaOrientation.Rotate270: newOrientation += 90; break;
      }

    if (newOrientation >= 360) newOrientation -= 360;

    switch (newOrientation) {
      case 0: mi.Orientation = (int)MediaOrientation.Normal; break;
      case 90: mi.Orientation = (int)MediaOrientation.Rotate90; break;
      case 180: mi.Orientation = (int)MediaOrientation.Rotate180; break;
      case 270: mi.Orientation = (int)MediaOrientation.Rotate270; break;
    }
  }
}