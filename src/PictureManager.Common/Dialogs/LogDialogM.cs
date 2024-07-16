using MH.UI.Controls;
using MH.Utils;

namespace PictureManager.Common.Dialogs;

public sealed class LogDialogM : Dialog {
  public LogDialogM() : base("Log", Res.IconSort) {
    Buttons = [
      new(new(() => { Log.Items.Clear(); Result = 1; }, null, "Clear"), true),
      new(CloseCommand, false, true)
    ];
  }
}