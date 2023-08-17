using MH.Utils.Extensions;

namespace MH.Utils.Interfaces {
  public interface ILeafyTreeItem<TP, TI, TL> : ITreeItem<TP, TI>
    where TP : class, ITreeItem<TP, TI>
    where TI : class, ITreeItem<TP, TI> {

    public ExtObservableCollection<TL> Leaves { get; set; }
  }
}
