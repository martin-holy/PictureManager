using System.Collections.ObjectModel;

namespace MH.Utils.BaseClasses {
  public class TreeWrapGroup : ObservableObject {
    private bool _isExpanded;

    public ObservableCollection<TreeWrapGroupInfoItem> Info { get; } = new();
    public ObservableCollection<object> Items { get; set; } = new();
    public ObservableCollection<object> WrappedItems { get; } = new();
    public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }
  }
}
