using MH.Utils.BaseClasses;

namespace MH.Utils.Interfaces;

public interface ILeafyTreeItem<T> : ITreeItem {
  public ExtObservableCollection<T> Leaves { get; set; }
}