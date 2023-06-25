using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MH.Utils.Extensions {
  public class ExtObservableCollection<T> : ObservableCollection<T> {
    public ExtObservableCollection() { }

    public ExtObservableCollection(IEnumerable<T> items) : base(items) { }

    public void Execute(Action<IList<T>> itemsAction) {
      itemsAction(Items);
      OnPropertyChanged(new("Count"));
      OnPropertyChanged(new("Item[]"));
      OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
    }
  }
}
