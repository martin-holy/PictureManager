using MH.Utils.BaseClasses;
using System;

namespace MH.UI.Controls {
  public class CollectionViewGroupByItem<T> : TreeItem {
    public object Parameter { get; }
    public Func<T, object, bool, Tuple<object, string>[]> ItemGroupBy { get; }

    public CollectionViewGroupByItem(string icon, string title, object parameter, Func<T, object, bool, Tuple<object, string>[]> itemGroupBy) {
      IconName = icon;
      Name = title;
      Parameter = parameter;
      ItemGroupBy = itemGroupBy;
    }
  }
}
