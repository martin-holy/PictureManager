using MH.Utils.Extensions;

namespace MH.Utils.Interfaces {
  public interface ITreeItemBase<TP, TI, TL>
    where TP : ITreeItemBase<TP, TI, TL>
    where TI : ITreeItemBase<TP, TI, TL> {

    public TP Parent { get; set; }
    public ExtObservableCollection<TI> Items { get; set; }
    public ExtObservableCollection<TL> Leaves { get; set; }
    public bool IsExpanded { get; set; }
  }
}
