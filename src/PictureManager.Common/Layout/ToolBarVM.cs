using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System.Collections.ObjectModel;

namespace PictureManager.Common.Layout;

public class ToolBarVM : ObservableObject {
  public ObservableCollection<object> ToolBars { get; } = [];
  public SlidePanelPinButton SlidePanelPinButton { get; } = new();
}