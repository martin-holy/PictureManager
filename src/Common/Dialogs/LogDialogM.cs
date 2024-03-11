using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;

namespace PictureManager.Common.Dialogs;

public sealed class LogDialogM : Dialog {
  public LogDialogM() : base("Log", Res.IconSort) {
    Buttons = new DialogButton[] {
      new(new(() => { Log.Items.Clear(); Result = 1; }, null, "Clear"), true),
      new(CloseCommand, false, true) };
  }
}