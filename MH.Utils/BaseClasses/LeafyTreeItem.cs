using MH.Utils.Extensions;
using MH.Utils.Interfaces;

namespace MH.Utils.BaseClasses {
  public class LeafyTreeItem<TP, TI, TL> : TreeItem<TP, TI>
    where TP : class, ITreeItem<TP, TI>
    where TI : class, ITreeItem<TP, TI> {

    public ExtObservableCollection<TL> Leaves { get; set; } = new();

    public LeafyTreeItem() { }
    public LeafyTreeItem(TP parent) : base(parent) { }
  }
}
