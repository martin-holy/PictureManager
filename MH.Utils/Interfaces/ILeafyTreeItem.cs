using MH.Utils.Extensions;

namespace MH.Utils.Interfaces {
  public interface ILeafyTreeItem<T> : ITreeItem {
    public ExtObservableCollection<T> Leaves { get; set; }
  }
}
