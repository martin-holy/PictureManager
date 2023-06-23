using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Dialogs {
  public class GroupByDialog<T> : Dialog {
    private bool _modeGroupBy = true;
    private bool _modeGroupByThenBy;
    private bool _modeGroupRecursive;

    public TreeItem Root { get; } = new();
    public Selecting<TreeItem> Selected { get; } = new();
    public Action<object, bool, bool> SelectAction => Select;
    public bool ModeGroupBy { get => _modeGroupBy; set { _modeGroupBy = value; OnPropertyChanged(); } }
    public bool ModeGroupByThenBy { get => _modeGroupByThenBy; set { _modeGroupByThenBy = value; OnPropertyChanged(); } }
    public bool ModeGroupRecursive { get => _modeGroupRecursive; set { _modeGroupRecursive = value; OnPropertyChanged(); } }

    public GroupByDialog() : base("Chose items for grouping", "IconGroup") {
      Buttons = new DialogButton[] {
        new("Ok", "IconCheckMark", YesOkCommand, true),
        new("Cancel", "IconXCross", CloseCommand, false, true) };
    }

    public void Open(CollectionViewGroup<T> group, IEnumerable<TreeItem> items) {
      ModeGroupBy = group.GroupMode is GroupMode.GroupBy or GroupMode.GroupByRecursive;
      ModeGroupByThenBy = group.GroupMode is GroupMode.ThanBy or GroupMode.ThanByRecursive;
      ModeGroupRecursive = group.GroupMode is GroupMode.GroupByRecursive or GroupMode.ThanByRecursive;
      Root.Items.Clear();
      Selected.DeselectAll();

      foreach (var item in items)
        Root.Items.Add(item);

      if (Show(this) != 1) return;

      if (Selected.Items.Count == 0) {
        group.Items.Clear();
        group.ReWrap();
        return;
      }

      group.GroupMode = ModeGroupByThenBy
        ? ModeGroupRecursive
          ? GroupMode.ThanByRecursive
          : GroupMode.ThanBy
        : ModeGroupRecursive
          ? GroupMode.GroupByRecursive
          : GroupMode.GroupBy;

      group.GroupByItems = Selected.Items.Cast<CollectionViewGroupByItem<T>>().ToArray();
      group.GroupIt();
      group.IsExpanded = true;
    }

    private void Select(object item, bool isCtrlOn, bool isShiftOn) {
      if (item is not TreeItem ti) return;
      Selected.Select(ti.Parent?.Items.Cast<TreeItem>().ToList(), ti, isCtrlOn, isShiftOn);
    }
  }
}
