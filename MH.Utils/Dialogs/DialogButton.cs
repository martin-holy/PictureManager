using MH.Utils.BaseClasses;

namespace MH.Utils.Dialogs;

public class DialogButton {
  public bool IsDefault { get; }
  public bool IsCancel { get; }
  public RelayCommand Command { get; }

  public DialogButton(RelayCommand command, bool isDefault = false, bool isCancel = false) {
    Command = command;
    IsDefault = isDefault;
    IsCancel = isCancel;
  }
}