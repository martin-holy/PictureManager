using MH.UI.WPF.Converters;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls;

public class Slot {
  public string Name { get; set; }
  public object Content { get; set; }
  public Dock Placement { get; set; }
}

public static class Slots {
  public static readonly DependencyProperty ListProperty = DependencyProperty.RegisterAttached(
    "ListInternal", typeof(IList), typeof(Slots));

  public static IList GetList(DependencyObject d) {
    var list = (IList)d.GetValue(ListProperty);
    if (list != null) return list;
    list = new List<Slot>();
    d.SetValue(ListProperty, list);
    return list;
  }

  public static void SetList(DependencyObject d, IList value) => d.SetValue(ListProperty, value);

  public static Slot GetSlot(object value, object parameter) =>
    value is DependencyObject d && d.GetValue(ListProperty) is IList<Slot> slots && parameter is string name
      ? slots.SingleOrDefault(x => x.Name.Equals(name))
      : null;
}

public class SlotContentConverter : BaseConverter {
  private static readonly object _lock = new();
  private static SlotContentConverter _inst;
  public static SlotContentConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) =>
    Slots.GetSlot(value, parameter)?.Content;
}

public class SlotPlacementConverter : BaseConverter {
  private static readonly object _lock = new();
  private static SlotPlacementConverter _inst;
  public static SlotPlacementConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) =>
    Slots.GetSlot(value, parameter)?.Placement;
}