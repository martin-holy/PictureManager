using MH.UI.Controls;

namespace PictureManager.Domain.Models {
  // TODO delete after mhc:SlidePanel is moved to MH.UI.Controls
  public sealed class ToolsTabsM : TabControl {
    private bool _isOpen;

    public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }

    public void Open() {
      if (!IsOpen) IsOpen = true;
    }
  }
}
