using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MH.UI.WPF.Utils;

public static class Init {
  public static void SetDelegates() {
    MH.Utils.Keyboard.IsCtrlOn = () => (Keyboard.Modifiers & ModifierKeys.Control) > 0;
    MH.Utils.Keyboard.IsAltOn = () => (Keyboard.Modifiers & ModifierKeys.Alt) > 0;
    MH.Utils.Keyboard.IsShiftOn = () => (Keyboard.Modifiers & ModifierKeys.Shift) > 0;

    MH.Utils.Clipboard.SetText = Clipboard.SetText;

    MH.Utils.Imaging.GetBitmapHashPixels = Imaging.GetBitmapHashPixels;
    MH.Utils.Imaging.ResizeJpg = Imaging.ResizeJpg;

    MH.UI.Controls.Dialog.Show = DialogHost.Show;

    MH.Utils.Tasks.Dispatch = action => Application.Current.Dispatcher.Invoke(DispatcherPriority.Render, action);

    CommandManager.RequerySuggested += RelayCommandBase.RaiseCanExecuteChanged;
  }
}