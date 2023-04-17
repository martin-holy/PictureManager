using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;

namespace PictureManager.Domain.Dialogs {
  public sealed class SettingsDialogM : Dialog {
    public SettingsDialogM() : base("Settings", Res.IconSettings) {
      Buttons = new DialogButton[] {
        new("Save", Res.IconSave, YesOkCommand, true),
        new("Close", Res.IconXCross, CloseCommand, false, true) };
    }
  }
}
