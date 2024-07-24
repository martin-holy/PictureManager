using MH.UI.Controls;
using MH.Utils;

namespace PictureManager.Common.Features.Common;

public sealed class LogDialog : Dialog {
  public LogDialog() : base("Log", Res.IconSort) {
    Buttons = [
      new(new(() => { Log.Items.Clear(); Result = 1; }, null, "Clear"), true),
      new(CloseCommand, false, true)
    ];
  }
}