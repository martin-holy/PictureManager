using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;

namespace PictureManager.Common.Features.Common;

public sealed class LogDialog : Dialog {
  public LogDialog() : base("Log", MH.UI.Res.IconSort) {
    Buttons = [
      new(new RelayCommand(() => { Log.Items.Clear(); Result = 1; }, null, "Clear"), true),
      new(CloseCommand, false, true)
    ];
  }
}