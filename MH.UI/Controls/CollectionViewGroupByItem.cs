using MH.Utils.BaseClasses;
using System;

namespace MH.UI.Controls {
  public class CollectionViewGroupByItem<T> : TreeItem {
    public object Parameter { get; }
    public Func<T, object, bool> ItemGroupBy { get; }

    public CollectionViewGroupByItem(string icon, string title, object parameter, Func<T, object, bool> itemGroupBy) {
      IconName = icon;
      Name = title;
      Parameter = parameter;
      ItemGroupBy = itemGroupBy;
    }
  }
}
