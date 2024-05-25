using MH.UI.WPF.Sample.Resources;
using MH.Utils.BaseClasses;

namespace MH.UI.WPF.Sample.ViewModels.Controls;

public class ButtonsVM : ObservableObject {
  public RelayCommand DialogButtonWithIconCommand { get; }
  public RelayCommand DialogButtonWithoutIconCommand { get; }

  public ButtonsVM() {
    DialogButtonWithIconCommand = new(() => { }, Icons.Image, "Sample");
    DialogButtonWithoutIconCommand = new(() => { }, null, "Sample");
  }
}