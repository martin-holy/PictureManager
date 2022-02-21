using System.Collections.ObjectModel;
using System.Linq;
using MH.UI.WPF.BaseClasses;
using MH.Utils.BaseClasses;

namespace PictureManager.ViewModels {
  public sealed class ToolsTabsVM : ObservableObject {
    private bool _isPinned;
    private bool _isOpen;
    private HeaderedListItem<object, string> _selected;

    public bool IsPinned { get => _isPinned; set { _isPinned = value; OnPropertyChanged(); } }
    public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }
    public HeaderedListItem<object, string> Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public ObservableCollection<HeaderedListItem<object, string>> Items { get; } = new();
    public RelayCommand<object> CloseTabCommand { get; }

    public ToolsTabsVM() {
      CloseTabCommand = new(() => Deactivate(Selected));
    }

    public void Activate(HeaderedListItem<object, string> item, bool open = false) {
      if (!Items.Contains(item))
        Items.Add(item);

      Selected = item;

      if (open && !IsOpen)
        IsOpen = true;
    }

    public void Deactivate(HeaderedListItem<object, string> item) {
      Items.Remove(item);
      Selected = Items.FirstOrDefault();

      if (Selected == null)
        IsPinned = false;
    }
  }
}
