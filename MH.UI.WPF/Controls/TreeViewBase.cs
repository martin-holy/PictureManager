using MH.UI.Interfaces;
using MH.UI.WPF.Extensions;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MH.UI.WPF.Controls;

public class TreeViewBase : TreeView {
  private bool _isScrollingTo;
  private bool _resetHScroll;
  private double _resetHOffset;
  private ScrollViewer _sv;

  public static readonly DependencyProperty TreeViewProperty = DependencyProperty.Register(
    nameof(TreeView), typeof(ITreeView), typeof(TreeViewBase));

  public ITreeView TreeView {
    get => (ITreeView)GetValue(TreeViewProperty);
    set => SetValue(TreeViewProperty, value);
  }

  public RelayCommand<RequestBringIntoViewEventArgs> TreeItemIntoViewCommand { get; set; }

  public TreeViewBase() {
    TreeItemIntoViewCommand = new(OnTreeItemIntoView);
  }

  public override void OnApplyTemplate() {
    base.OnApplyTemplate();

    _sv = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);
    _sv.IsVisibleChanged += delegate { SetIsVisible(); };
    _sv.ScrollChanged += OnScrollChanged;
    SetIsVisible();

    if (TreeView == null) return;
    SetItemsSource();
    TreeView.ScrollToTopAction = ScrollToTop;
    TreeView.ScrollToItemsAction = ScrollToItemsWhenReady;
    TreeView.ExpandRootWhenReadyAction = ExpandRootWhenReady;
  }

  private void SetIsVisible() {
    if (TreeView != null) TreeView.IsVisible = _sv.IsVisible;
  }

  private void OnTreeItemIntoView(RequestBringIntoViewEventArgs e) {
    _resetHScroll = true;
    _resetHOffset = _sv.HorizontalOffset;
  }

  public virtual void OnScrollChanged(object sender, ScrollChangedEventArgs e) {
    if (!_isScrollingTo && TreeView != null && e.VerticalChange != 0 && _sv.IsVisible)
      TreeView.TopTreeItem = GetHitTestItem(10, 10);

    if (_resetHScroll) {
      _resetHScroll = false;
      _sv.ScrollToHorizontalOffset(_resetHOffset);
    }
  }

  private void SetItemsSource() {
    var expand = false;
    var root = TreeView.RootHolder.FirstOrDefault() as ITreeItem;
    if (root is { IsExpanded: true }) {
      expand = true;
      root.IsExpanded = false;
    }
    ItemsSource = TreeView.RootHolder;
    if (expand) ExpandRootWhenReady(root);
  }

  private void ScrollToTop() {
    if (_sv == null || _sv.VerticalOffset == 0) return;
    _sv.ScrollToTop();
    _sv.UpdateLayout();
  }

  private void ScrollToItemsWhenReady(object[] items, bool exactly) {
    Dispatcher.BeginInvoke(DispatcherPriority.Background, () => ScrollToItems(items, exactly));
  }

  private void ScrollToItems(object[] items, bool exactly) {
    var root = (ITreeItem)TreeView.RootHolder[0];
    var idxItem = ((ITreeItem)items[^1]).GetIndex(root);

    if (idxItem < 0 || !GetDiff(idxItem, root, out var diff) || diff == 0) return;

    _isScrollingTo = true;

    if (IsDiffInView(idxItem, root)) {
      if (!exactly) {
        _isScrollingTo = false;
        return;
      }
      _sv.ScrollToVerticalOffset(_sv.VerticalOffset + diff);
    }

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
    var bottomY = ActualHeight - 10;
    if (_sv.ComputedHorizontalScrollBarVisibility == Visibility.Visible) bottomY -= 14;
    var idxTopItem = GetHitTestItem(10, 10)?.GetIndex(root);
    var idxBottomItem = GetHitTestItem(10, bottomY)?.GetIndex(root);
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
  /// TreeView loads all items when everything is expanded.
  /// So I collapsed the root on reload and expanded it after to load just what is in the view.
  /// </summary>
  private void ExpandRootWhenReady(ITreeItem root) {
    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => {
      root.IsExpanded = true;
    });
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