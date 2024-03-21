using MH.UI.Controls;

namespace PictureManager.Plugins.Common.Interfaces.ViewModels;

public interface IPluginHostCoreVM {
  public TabControl MainTabs { get; }
  public TabControl ToolsTabs { get; }
}