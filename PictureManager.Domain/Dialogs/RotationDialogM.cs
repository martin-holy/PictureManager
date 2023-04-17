using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Dialogs {
  public sealed class RotationDialogM : Dialog {
    public RelayCommand<MediaOrientation> RotationCommand { get; }

    public RotationDialogM() : base("Rotation", Res.IconImageMultiple) {
      RotationCommand = new(mo => Result = (int)mo);
    }
  }
}
