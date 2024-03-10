using System.Collections.Generic;

namespace MH.Utils.BaseClasses; 

public class SelectionEventArgs<T> {
  public List<T> Items { get; }
  public T Item { get; }
  public bool IsCtrlOn { get; }
  public bool IsShiftOn { get; }

  public SelectionEventArgs(List<T> items, T item, bool isCtrlOn, bool isShiftOn) {
    Items = items;
    Item = item;
    IsCtrlOn = isCtrlOn;
    IsShiftOn = isShiftOn;
  }
}