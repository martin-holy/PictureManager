using MH.Utils.BaseClasses;

namespace MH.Utils.Interfaces;

public interface ITreeItem : IListItem {
  public ITreeItem? Parent { get; set; }
  public ExtObservableCollection<ITreeItem> Items { get; }
  public bool IsExpanded { get; set; }
}