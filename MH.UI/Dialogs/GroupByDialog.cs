using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Dialogs {
  public class GroupByDialog<T> : Dialog where T : ISelectable {
    private bool _isRecursive;
    private bool _isGroupBy = true;
    private bool _isThenBy;

    public TreeView<CollectionViewGroupByItem<T>> TreeView { get; } = new() { ShowTreeItemSelection = true };
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
      TreeView.RootHolder.Clear();
      TreeView.SelectedTreeItems.DeselectAll();

      foreach (var item in items)
        TreeView.RootHolder.Add(item);

      if (Show(this) != 1) return false;

      if (TreeView.SelectedTreeItems.Items.Count == 0) {
        group.GroupByItems = null;
        group.Items.Clear();
        group.ReWrap();
        return true;
      }

      group.IsRoot = true;
      group.IsRecursive = IsRecursive;
      group.IsGroupBy = IsGroupBy;
      group.IsThenBy = IsThenBy;
      group.GroupByItems = TreeView.SelectedTreeItems.Items.ToArray();
      CollectionViewGroup<T>.GroupIt(group);
      group.View.RemoveEmptyGroups(group, null);
      group.IsExpanded = true;

      return true;
    }
  }
}
