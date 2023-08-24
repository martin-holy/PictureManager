using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.UI.Dialogs {
  public class GroupByDialog<T> : Dialog where T : ISelectable {
    private bool _isRecursive;
    private bool _isGroupBy = true;
    private bool _isThenBy;

    public ObservableCollection<CollectionViewGroupByItem<T>> RootItems { get; } = new();
    public Selecting<CollectionViewGroupByItem<T>> Selected { get; } = new();
    public Action<object, bool, bool> SelectAction => Select;
    public bool IsRecursive { get => _isRecursive; set { _isRecursive = value; OnPropertyChanged(); } }
    public bool IsGroupBy { get => _isGroupBy; set { _isGroupBy = value; OnPropertyChanged(); } }
    public bool IsThenBy { get => _isThenBy; set { _isThenBy = value; OnPropertyChanged(); } }

    public GroupByDialog() : base("Chose items for grouping", "IconGroup") {
      Buttons = new DialogButton[] {
        new("Ok", "IconCheckMark", YesOkCommand, true),
        new("Cancel", "IconXCross", CloseCommand, false, true) };
    }

    public bool Open(ICollectionViewGroup<T> group, IEnumerable<CollectionViewGroupByItem<T>> items) {
      IsRecursive = group.IsRecursive;
      IsGroupBy = group.IsGroupBy;
      IsThenBy = group.IsThenBy;
      RootItems.Clear();
      Selected.DeselectAll();

      foreach (var item in items)
        RootItems.Add(item);

      if (Show(this) != 1) return false;

      if (Selected.Items.Count == 0) {
        group.GroupByItems = null;
        group.Items.Clear();
        group.ReWrap();
        return true;
      }

      group.IsRoot = true;
      group.IsRecursive = IsRecursive;
      group.IsGroupBy = IsGroupBy;
      group.IsThenBy = IsThenBy;
      group.GroupByItems = Selected.Items.ToArray();
      CollectionViewGroup<T>.GroupIt(group);
      group.View.RemoveEmptyGroups(group, null);
      group.IsExpanded = true;

      return true;
    }

    private void Select(object item, bool isCtrlOn, bool isShiftOn) {
      if (item is not CollectionViewGroupByItem<T> ti) return;
      Selected.Select(ti.Parent?.Items.Cast<CollectionViewGroupByItem<T>>().ToList(), ti, isCtrlOn, isShiftOn);
    }
  }
}
