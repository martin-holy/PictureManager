using MH.UI.Controls;
using MH.UI.Dialogs;

namespace PictureManager.Plugins.Common.Interfaces.ViewModels;

public interface IPluginHostCoreVM {
  public TabControl MainTabs { get; }
  public TabControl ToolsTabs { get; }
  public ToggleDialog ToggleDialog { get; }
}