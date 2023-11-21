using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls; 

public class TreeView<T> : ObservableObject, ITreeView where T : ITreeItem {
  private ITreeItem _topTreeItem;

  public ExtObservableCollection<object> RootHolder { get; } = new();
  public Selecting<T> SelectedTreeItems { get; } = new();
  public ITreeItem TopTreeItem { get => _topTreeItem; set { _topTreeItem = value; OnTopTreeItemChanged(); } }
  // TODO rename and combine with single and multi select
  public bool ShowTreeItemSelection { get; set; }
  public Action ScrollToTopAction { get; set; }
  public Action<object[], bool> ScrollToItemsAction { get; set; }
  public Action<ITreeItem> ExpandRootWhenReadyAction { get; set; }

  public RelayCommand<object> TreeItemSelectedCommand { get; }
  public event EventHandler<ObjectEventArgs<T>> TreeItemSelectedEvent = delegate { };

  public TreeView() {
    TreeItemSelectedCommand = new(OnTreeItemSelected);
  }

  public virtual void OnIsVisible() =>
    ScrollTo(TopTreeItem);

  public virtual void OnTreeItemSelected(object o) {
    if (o is not T t) return;
    TreeItemSelectedEvent(this, new(t));
    if (!ShowTreeItemSelection) return;
    SelectedTreeItems.Select(t.Parent?.Items.Cast<T>().ToList(), t, Keyboard.IsCtrlOn(), Keyboard.IsShiftOn());
  }

  public virtual void OnTopTreeItemChanged() { }

  public virtual void ScrollTo(ITreeItem item, bool exactly = true) {
    if (item == null) return;

    var branch = item.GetBranch();
    for (int i = 0; i < branch.Count - 1; i++)
      branch[i].IsExpanded = true;

    TopTreeItem = item;
    ScrollToItemsAction?.Invoke(branch.Cast<object>().ToArray(), exactly);
  }

  public void UpdateRoot(ITreeItem root, Action<IList<object>> itemsAction) {
    var expand = false;
    RootHolder.Execute(items => {
      items.Clear();
      itemsAction?.Invoke(items);
      expand = root.IsExpanded;
      if (expand) root.IsExpanded = false;
      items.Add(root);
    });

    if (!expand) return;

    if (ExpandRootWhenReadyAction == null)
      root.IsExpanded = true;
    else
      ExpandRootWhenReadyAction(root);
  }
}