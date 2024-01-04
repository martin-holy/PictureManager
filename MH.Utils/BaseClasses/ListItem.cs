using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MH.Utils.BaseClasses;

public class ListItem : ObservableObject, IListItem {
  protected BitVector32 Bits = new(0);

  private string _icon;
  private string _name;

  public bool IsSelected { get => Bits[BitsMasks.IsSelected]; set { Bits[BitsMasks.IsSelected] = value; OnPropertyChanged(); } }
  public bool IsHidden { get => Bits[BitsMasks.IsHidden]; set { Bits[BitsMasks.IsHidden] = value; OnPropertyChanged(); } }
  public bool IsIconHidden { get => Bits[BitsMasks.IsIconHidden]; set { Bits[BitsMasks.IsIconHidden] = value; OnPropertyChanged(); } }
  public bool IsNameHidden { get => Bits[BitsMasks.IsNameHidden]; set { Bits[BitsMasks.IsNameHidden] = value; OnPropertyChanged(); } }
  public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
  public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
  public object Data { get; set; }

  public ListItem() { }

  public ListItem(string icon, string name) : this() {
    Icon = icon;
    Name = name;
  }

  public ListItem(string icon, string name, object data) : this(icon, name) {
    Data = data;
  }
}

public class ListItem<T> : ListItem {
  private T _content;

  public T Content { get => _content; set { _content = value; OnPropertyChanged(); } }

  public ListItem(T content) {
    Content = content;
  }

  public ListItem(T content, string icon, string name) : base(icon, name) {
    Content = content;
  }
}

public static class ListItemExtensions {
  public static T GetByName<T>(this IEnumerable<T> items, string name, StringComparison sc = StringComparison.Ordinal)
    where T : IListItem =>
    items.SingleOrDefault(x => x.Name.Equals(name, sc));
}