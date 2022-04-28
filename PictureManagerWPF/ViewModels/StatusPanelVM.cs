using MH.UI.WPF.BaseClasses;

namespace PictureManager.ViewModels {
  public static class StatusPanelVM  {
    public static RelayCommand<object> PinCommand { get; } = new(
      () => App.Core.StatusPanelM.IsPinned = !App.Core.StatusPanelM.IsPinned);
  }
}
