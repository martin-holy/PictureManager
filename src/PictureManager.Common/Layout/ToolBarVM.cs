using MH.UI.Controls;
using MH.Utils.BaseClasses;

namespace PictureManager.Common.Layout;

public class ToolBarVM : ObservableObject {
  public SlidePanelPinButton SlidePanelPinButton { get; } = new();
}