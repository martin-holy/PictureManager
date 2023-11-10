using MH.UI.Interfaces;
using MH.UI.WPF.Utils;
using MH.Utils;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MH.UI.WPF.Controls;

public class TreeViewBase : TreeView {
  private bool _isScrollingTo;
  private ScrollViewer _sv;

  public static readonly DependencyProperty TreeViewProperty = DependencyProperty.Register(
    nameof(TreeView), typeof(ITreeView), typeof(TreeViewBase));

  public ITreeView TreeView {
    get => (ITreeView)GetValue(TreeViewProperty);
    set => SetValue(TreeViewProperty, value);
  }

  public override void OnApplyTemplate() {
    base.OnApplyTemplate();

    _sv = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);
    _sv.IsVisibleChanged += delegate { if (_sv.IsVisible) TreeView?.OnIsVisible(); };
    _sv.ScrollChanged += (_, e) => {
      if (!_isScrollingTo && TreeView != null && e.VerticalChange != 0 && _sv.IsVisible)
        TreeView.TopTreeItem = GetHitTestItem(10, 10);
    };

    if (TreeView == null) return;
    ItemsSource = TreeView.RootHolder;
    TreeView.ScrollToTopAction = ScrollToTop;
    TreeView.ScrollToItemsAction = ScrollToItems;
  }

  private void ScrollToTop() {
    if (_sv == null || _sv.VerticalOffset == 0) return;
    _sv.ScrollToTop();
    _sv.UpdateLayout();
  }

  private void ScrollToItems(object[] items) {
    var root = (ITreeItem)TreeView.RootHolder[0];
    var idxItem = ((ITreeItem)items[^1]).GetIndex(root);

    if (idxItem < 0 || !GetDiff(idxItem, root, out var diff) || diff == 0) return;

    _isScrollingTo = true;

    if (IsDiffInView(idxItem, root)) 
      _sv.ScrollToVerticalOffset(_sv.VerticalOffset + diff);

    _sv.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => {
      if (!GetDiff(idxItem, root, out diff) || diff == 0) return;

      // if diff wasn't it the view
      ItemsControl parent = ScrollIntoView(this, items);

      parent?.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => {
        if (!GetDiff(idxItem, root, out diff) || diff == 0) return;
        _sv.ScrollToVerticalOffset(_sv.VerticalOffset + diff);
        _sv.Dispatcher.BeginInvoke(DispatcherPriority.Background, () => {
          _isScrollingTo = false;
        });
      });
    });
  }

  private bool GetDiff(int idxItem, ITreeItem root, out int diff) {
    diff = 0;
    var flag = false;
    var idxTopItem = GetHitTestItem(10, 10)?.GetIndex(root);

    if (idxTopItem is not (null or < 0)) {
      diff = idxItem - (int)idxTopItem;
      flag = true;
    }

    if (diff == 0) _isScrollingTo = false;
    return flag;
  }

  private bool IsDiffInView(int idxItem, ITreeItem root) {
    var idxTopItem = GetHitTestItem(10, 10)?.GetIndex(root);
    var idxBottomItem = GetHitTestItem(10, ActualHeight - 10)?.GetIndex(root);
    return idxTopItem is not (null or < 0) && idxBottomItem is not (null or < 0) &&
           idxTopItem <= idxItem && idxItem <= idxBottomItem;
  }

  private static ItemsControl ScrollIntoView(ItemsControl parent, IEnumerable<object> items) {
    foreach (var item in items) {
      var index = parent.Items.IndexOf(item);
      if (index < 0) break;
      var panel = parent.GetChildOfType<VirtualizingStackPanel>();
      if (panel == null) break;
      panel.BringIndexIntoViewPublic(index);
      panel.UpdateLayout();
      if (parent.ItemContainerGenerator.ContainerFromIndex(index) is not TreeViewItem tvi) break;
      parent = tvi;
    }

    return parent;
  }

  private ITreeItem GetHitTestItem(double x, double y) {
    ITreeItem outItem = null;
    VisualTreeHelper.HitTest(_sv, null, e => {
      if (e.VisualHit is not FrameworkElement { DataContext: ITreeItem item })
        return HitTestResultBehavior.Continue;

      outItem = item;
      return HitTestResultBehavior.Stop;

    }, new PointHitTestParameters(new(x, y)));

    return outItem;
  }

  /// <summary>
  /// Scroll TreeView when the mouse is near the top or bottom
  /// </summary>
  public void DragDropAutoScroll(DragEventArgs e) {
    const int px = 25;
    var unit = GetValue(VirtualizingPanel.ScrollUnitProperty) is ScrollUnit.Item ? 1 : px;
    var pos = e.GetPosition(this);
    if (pos.Y < px)
      _sv.ScrollToVerticalOffset(_sv.VerticalOffset - unit);
    else if (ActualHeight - pos.Y < px)
      _sv.ScrollToVerticalOffset(_sv.VerticalOffset + unit);
  }
}