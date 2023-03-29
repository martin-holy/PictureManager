using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class ToolsTabsM : ObservableObject {
    private bool _isOpen;
    private HeaderedListItem<object, string> _selected;

    public bool IsOpen { get => _isOpen; set { _isOpen = value; OnPropertyChanged(); } }
    public HeaderedListItem<object, string> Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public ObservableCollection<HeaderedListItem<object, string>> Items { get; } = new();
    public RelayCommand<object> CloseTabCommand { get; }

    public ToolsTabsM() {
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
      if (Items.Remove(item) && Selected == item)
        Selected = Items.FirstOrDefault();
    }
  }
}
