using MH.Utils.BaseClasses;
using System.Windows.Input;

namespace MH.UI.Controls;

public class DialogButton(ICommand command, bool isDefault = false, bool isCancel = false) : ObservableObject {
  public bool IsDefault { get; } = isDefault;
  public bool IsCancel { get; } = isCancel;
  public ICommand Command { get; } = command;
}