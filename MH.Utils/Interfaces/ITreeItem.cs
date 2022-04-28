using System.Collections.ObjectModel;

namespace MH.Utils.Interfaces {
  public interface ITreeItem : IListItem {
    ITreeItem Parent { get; set; }
    ObservableCollection<ITreeItem> Items { get; set; }
    bool IsExpanded { get; set; }
    bool HasThisParent(ITreeItem parent);
    void ExpandAll();
    void ExpandTo();
  }
}
