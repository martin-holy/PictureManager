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
      NotifyChange(NotifyCollectionChangedAction.Reset, null);
    }

    public void AddItems(IList<T> items, Action<T> itemAction) {
      foreach (var item in items) {
        itemAction?.Invoke(item);
        Items.Add(item);
      }

      NotifyChange(NotifyCollectionChangedAction.Add, items);
    }

    public void RemoveItems(IList<T> items, Action<T> itemAction) {
      foreach (var item in items) {
        itemAction?.Invoke(item);
        Items.Remove(item);
      }

      NotifyChange(NotifyCollectionChangedAction.Remove, items);
    }

    private void NotifyChange(NotifyCollectionChangedAction action, IList<T> items) {
      OnPropertyChanged(new("Count"));
      OnPropertyChanged(new("Item[]"));

      if (items == null)
        OnCollectionChanged(new(NotifyCollectionChangedAction.Reset));
      else
        OnCollectionChanged(new(action, items));
    }
  }
}
