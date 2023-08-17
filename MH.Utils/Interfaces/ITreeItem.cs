using MH.Utils.Extensions;
using System.Collections.Generic;

namespace MH.Utils.Interfaces {
  public interface ITreeItem<TP, TI> : ISelectable
    where TP : class, ITreeItem<TP, TI>
    where TI : class, ITreeItem<TP, TI> {

    public TP Parent { get; set; }
    public ExtObservableCollection<TI> Items { get; set; }
    public bool IsExpanded { get; set; }

    public IEnumerable<T> GetThisAndParents<T>() where T : ITreeItem<TP, TI>;
    public void SetExpanded<T>(bool value) where T : ITreeItem<TP, TI>;
    public void ExpandTo();
  }

  public interface ITreeItem : ITreeItem<ITreeItem, ITreeItem>, IIconText, ITitled {
    public bool IsHidden { get; set; }
  }
}
