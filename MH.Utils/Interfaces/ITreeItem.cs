using MH.Utils.Extensions;

namespace MH.Utils.Interfaces {
  public interface ITreeItem : IListItem {
    public ITreeItem Parent { get; set; }
    public ExtObservableCollection<ITreeItem> Items { get; set; }
    public bool IsExpanded { get; set; }
  }
}
