﻿using MH.UI.Interfaces;
using MH.UI.WPF.Utils;
using MH.Utils;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DispatcherPriority = System.Windows.Threading.DispatcherPriority;

namespace MH.UI.WPF.Controls {
  public class TreeViewBase : TreeView {
    private bool _isScrollingTo;
    private object[] _pendingScrollToItems;

    public ScrollViewer ScrollViewer { get; set; }

    public static readonly DependencyProperty TreeViewProperty = DependencyProperty.Register(
      nameof(TreeView), typeof(ITreeView), typeof(TreeViewBase));

    public ITreeView TreeView {
      get => (ITreeView)GetValue(TreeViewProperty);
      set => SetValue(TreeViewProperty, value);
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      ScrollViewer = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);

      ScrollViewer.ScrollChanged += (_, e) => {
        if (e.VerticalChange == 0) return;
        if (TreeView is { IsSizeChanging: false } && !_isScrollingTo)
          TreeView.TopTreeItem = GetTopItem();
      };

      if (TreeView != null) {
        ItemsSource = TreeView.RootHolder;

        TreeView.ScrollToTopAction = ScrollToTop;
        TreeView.ScrollToItemsAction = items => ScrollToItems(items.ToArray());
      }

      LayoutUpdated += (_, _) => {
        if (TreeView?.IsSizeChanging == true)
          TreeView.IsSizeChanging = false;
      };

      SizeChanged += (_, e) => {
        if (TreeView != null && e.PreviousSize is not { Width: 0, Height: 0 })
          TreeView.IsSizeChanging = true;
      };

      IsVisibleChanged += (_, _) => {
        if (!IsVisible || _pendingScrollToItems == null) return;
        ScrollToItems(_pendingScrollToItems);
        _pendingScrollToItems = null;
      };
    }

    private void ScrollToItems(object[] items) {
      if (!IsVisible || ScrollViewer == null) {
        _pendingScrollToItems = items;
        return;
      }

      var item = items[^1] as ITreeItem;
      if (!ScrollToDiff(item, out var diff)) return;

      _isScrollingTo = true;
      ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + diff);
      ScrollViewer.Dispatcher.BeginInvoke(DispatcherPriority.Background, () => {
        if (!ScrollToDiff(item, out diff)) return;

        // if soft scroll did not work
        ItemsControl parent = ScrollIntoView(this, items);

        parent.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => {
          if (!ScrollToDiff(item, out diff)) return;

          ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + diff);
          ScrollViewer.Dispatcher.BeginInvoke(DispatcherPriority.Background, () => {
            _isScrollingTo = false;
          });
        });
      });
    }

    private bool ScrollToDiff(ITreeItem item, out int diff) {
      var root = (ITreeItem)TreeView.RootHolder[0];
      var itemIndex = item?.GetIndex(root);
      var topItemIndex = GetTopItem()?.GetIndex(root);

      diff = itemIndex is not (null or < 0) && topItemIndex is not (null or < 0)
        ? (int)itemIndex - (int)topItemIndex
        : 0;

      if (diff == 0) _isScrollingTo = false;

      return diff != 0;
    }

    private ItemsControl ScrollIntoView(ItemsControl parent, IEnumerable<object> items) {
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

    private void ScrollToTop() {
      if (ScrollViewer == null || ScrollViewer.VerticalOffset == 0) return;
      ScrollViewer.ScrollToTop();
      ScrollViewer.UpdateLayout();
    }

    private ITreeItem GetTopItem() {
      ITreeItem outItem = null;
      VisualTreeHelper.HitTest(this, null, e => {
        if (e.VisualHit is not FrameworkElement { DataContext: ITreeItem item })
          return HitTestResultBehavior.Continue;

        outItem = item;
        return HitTestResultBehavior.Stop;

      }, new PointHitTestParameters(new(10, 10)));

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
        ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - unit);
      else if (ActualHeight - pos.Y < px)
        ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + unit);
    }
  }
}
