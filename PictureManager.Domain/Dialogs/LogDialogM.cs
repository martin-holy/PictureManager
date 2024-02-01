using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;

namespace PictureManager.Domain.Dialogs {
  public sealed class LogDialogM : Dialog {
    public LogDialogM() : base("Log", Res.IconSort) {
      Buttons = new DialogButton[] {
        new("Clear", null, new RelayCommand(() => { Log.Items.Clear(); Result = 1; }), true),
        new("Close", Res.IconXCross, CloseCommand, false, true) };
    }
  }
}
