using MH.UI.Controls;
using MH.UI.Dialogs;

namespace PictureManager.Interfaces.ViewModels;

public interface ICoreVM {
  public TabControl MainTabs { get; }
  public TabControl ToolsTabs { get; }
  public ToggleDialog ToggleDialog { get; }
  public ISegmentVM Segment { get; }
}