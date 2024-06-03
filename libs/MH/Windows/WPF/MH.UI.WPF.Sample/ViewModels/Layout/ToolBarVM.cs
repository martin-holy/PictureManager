using MH.UI.Controls;
using MH.Utils.BaseClasses;

namespace MH.UI.WPF.Sample.ViewModels.Layout;

public sealed class ToolBarVM : ObservableObject {
  public SlidePanelPinButton SlidePanelPinButton { get; } = new();
}