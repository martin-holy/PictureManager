using MH.Utils.Extensions;

namespace MH.Utils.Interfaces {
  public interface ITreeItem : ISelectable, IIconText, ITitled {
    public ITreeItem Parent { get; set; }
    public ExtObservableCollection<ITreeItem> Items { get; set; }
    public bool IsExpanded { get; set; }
    public object Data { get; }
  }
}
