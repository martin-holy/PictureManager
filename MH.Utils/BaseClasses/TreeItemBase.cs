using MH.Utils.Extensions;
using MH.Utils.Interfaces;

namespace MH.Utils.BaseClasses {
  public class TreeItemBase<TP, TI, TL> : ObservableObject, ITreeItemBase<TP, TI, TL>
    where TP : ITreeItemBase<TP, TI, TL>
    where TI : ITreeItemBase<TP, TI, TL> {

    private TP _parent;
    private bool _isExpanded;

    public TP Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }
    public ExtObservableCollection<TI> Items { get; set; } = new();
    public ExtObservableCollection<TL> Leaves { get; set; } = new();
    public bool IsExpanded {
      get => _isExpanded;
      set {
        _isExpanded = value;
        OnIsExpandedChanged(value);
        OnPropertyChanged();
      }
    }

    public TreeItemBase(TP parent) {
      Parent = parent;
    }

    public virtual void OnIsExpandedChanged(bool value) { }
  }
}
