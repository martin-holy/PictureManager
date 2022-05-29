using MH.Utils.HelperClasses;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class VirtualizingWrapPanelRow {
    public ObservableCollection<object> Items { get; } = new();
  }

  public class VirtualizingWrapPanel : ItemsControl {
    private UIElement _rowToScrollToTop;
    private VirtualizingStackPanel _rowsStackPanel;
    private object _topItem;
    private object _topItemBeforeReload;

    public ScrollViewer RowsScrollViewer { get; set; }

    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
      nameof(ItemWidth),
      typeof(double),
      typeof(VirtualizingWrapPanel));

    public static readonly DependencyProperty ItemWidthGetterProperty = DependencyProperty.Register(
      nameof(ItemWidthGetter),
      typeof(Func<object, int>),
      typeof(VirtualizingWrapPanel));

    public static readonly DependencyProperty ItemsToWrapProperty = DependencyProperty.Register(
      nameof(ItemsToWrap),
      typeof(ObservableCollection<object>),
      typeof(VirtualizingWrapPanel),
      new(ItemsToWrapChanged));

    public static readonly DependencyProperty ScrollToItemProperty = DependencyProperty.Register(
      nameof(ScrollToItem),
      typeof(object),
      typeof(VirtualizingWrapPanel),
      new(ScrollToItemChanged));

    public static readonly DependencyProperty ScrollToTopProperty = DependencyProperty.Register(
      nameof(ScrollToTop),
      typeof(bool),
      typeof(VirtualizingWrapPanel),
      new(ScrollToTopChanged));

    public static readonly DependencyProperty WrappedItemsProperty = DependencyProperty.Register(
      nameof(WrappedItems),
      typeof(ObservableCollection<object>),
      typeof(VirtualizingWrapPanel));

    public static readonly DependencyProperty ScrollViewerSpeedFactorProperty = DependencyProperty.Register(
      nameof(ScrollViewerSpeedFactor),
      typeof(double),
      typeof(VirtualizingWrapPanel),
      new(2.5));

    public static readonly DependencyProperty ReloadAutoScrollProperty = DependencyProperty.Register(
      nameof(ReloadAutoScroll),
      typeof(bool),
      typeof(VirtualizingWrapPanel),
      new(true));

    public double ItemWidth {
      get => (double)GetValue(ItemWidthProperty);
      set => SetValue(ItemWidthProperty, value);
    }

    public Func<object, int> ItemWidthGetter {
      get => (Func<object, int>)GetValue(ItemWidthGetterProperty);
      set => SetValue(ItemWidthGetterProperty, value);
    }

    public ObservableCollection<object> ItemsToWrap {
      get => (ObservableCollection<object>)GetValue(ItemsToWrapProperty);
      set => SetValue(ItemsToWrapProperty, value);
    }

    public object ScrollToItem {
      get => GetValue(ScrollToItemProperty);
      set => SetValue(ScrollToItemProperty, value);
    }

    public bool ScrollToTop {
      get => (bool)GetValue(ScrollToTopProperty);
      set => SetValue(ScrollToTopProperty, value);
    }

    public ObservableCollection<object> WrappedItems {
      get => (ObservableCollection<object>)GetValue(WrappedItemsProperty);
      set => SetValue(WrappedItemsProperty, value);
    }

    public double ScrollViewerSpeedFactor {
      get => (double)GetValue(ScrollViewerSpeedFactorProperty);
      set => SetValue(ScrollViewerSpeedFactorProperty, value);
    }

    public bool ReloadAutoScroll {
      get => (bool)GetValue(ReloadAutoScrollProperty);
      set => SetValue(ReloadAutoScrollProperty, value);
    }

    static VirtualizingWrapPanel() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(VirtualizingWrapPanel),
        new FrameworkPropertyMetadata(typeof(VirtualizingWrapPanel)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      var itemsPresenter = (ItemsPresenter)Template.FindName("PART_ItemsPresenter", this);
      itemsPresenter.ApplyTemplate();

      _rowsStackPanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as VirtualizingStackPanel;
      RowsScrollViewer = (ScrollViewer)Template.FindName("PART_RowsScrollViewer", this);

      LayoutUpdated += (_, _) => {
        if (_topItemBeforeReload != null) {
          ScrollTo(_topItemBeforeReload);
          _topItemBeforeReload = null;
        }

        if (_rowToScrollToTop == null) return;
        RowsScrollViewer?.ScrollToVerticalOffset(_rowsStackPanel.GetItemOffset(_rowToScrollToTop));
        _rowToScrollToTop = null;
      };

      RowsScrollViewer.ScrollChanged += (_, _) => {
        _topItem = null;

        if (Items.Count > 0) {
          var item = Items[GetTopRowIndex()];

          _topItem = item is VirtualizingWrapPanelRow row && row.Items.Count > 0
            ? row.Items[0]
            : item is ItemsGroup group && group.Items.Count > 0
              ? group.Items[0]
              : null;
        }
      };

      Loaded += (_, _) =>
        AddAll();
    }

    private static void ScrollToItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not VirtualizingWrapPanel vwp) return;
      vwp.ScrollTo(e.NewValue);
      // reset so that scroll to same item is possible
      vwp.ScrollToItem = null;
    }

    private static void ScrollToTopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not VirtualizingWrapPanel vwp || !(bool)e.NewValue) return;
      vwp.RowsScrollViewer?.ScrollToTop();
      vwp.RowsScrollViewer?.UpdateLayout();
      vwp.ScrollToTop = false;
    }

    private static void ItemsToWrapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      var panel = (VirtualizingWrapPanel)d;
      panel.WrappedItems = new();
      panel.AddAll();

      if (e.OldValue is ObservableCollection<object> oldValue)
        oldValue.CollectionChanged -= panel.ItemsToWrapCollectionChanged;

      if (e.NewValue is ObservableCollection<object> newValue)
        newValue.CollectionChanged += panel.ItemsToWrapCollectionChanged;
    }

    private void ItemsToWrapCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      switch (e.Action) {
        case NotifyCollectionChangedAction.Add:
        if (e.NewItems != null)
          foreach (var item in e.NewItems)
            if (item is ItemsGroup group) {
              group.Items.CollectionChanged += ItemsGroupItemsCollectionChanged;
              WrappedItems.Add(group);
            }
            else
              AddItem(item);
        break;

        case NotifyCollectionChangedAction.Reset:
        foreach (var group in WrappedItems.OfType<ItemsGroup>())
          group.Items.CollectionChanged -= ItemsGroupItemsCollectionChanged;

        if (ReloadAutoScroll)
          _topItemBeforeReload = _topItem;
        else
          RowsScrollViewer.ScrollToTop();

        WrappedItems.Clear();
        break;

        default:
        WrappedItems.Clear();
        AddAll();
        break;
      }
    }

    private void ItemsGroupItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        foreach (var item in e.NewItems)
          AddItem(item);
      else
        ReWrap();
    }

    private void AddAll() {
      if (ActualWidth != 0 && ItemsToWrap != null && ItemsToWrap.Count != 0 && WrappedItems.Count == 0)
        foreach (var item in ItemsToWrap) {
          if (item is ItemsGroup group) {
            WrappedItems.Add(group);
            foreach (var groupItem in group.Items)
              AddItem(groupItem);
          }
          else
            AddItem(item);
        }
    }

    private void AddItem(object item) {
      VirtualizingWrapPanelRow row = null;

      if (WrappedItems.Count > 0)
        row = WrappedItems[^1] as VirtualizingWrapPanelRow;

      if (row == null) {
        row = new();
        WrappedItems.Add(row);
      }

      var usedSpace = ItemWidthGetter != null
        ? row.Items.Sum(x => ItemWidthGetter(x))
        : row.Items.Count * ItemWidth;

      var itemWidth = ItemWidthGetter?.Invoke(item) ?? ItemWidth;

      if (ActualWidth - usedSpace < itemWidth) {
        row = new();
        WrappedItems.Add(row);
      }

      row.Items.Add(item);
    }

    public void ReWrap() {
      WrappedItems.Clear();
      AddAll();
      ScrollTopRowOrItem();
    }

    private void ScrollTopRowOrItem() {
      if (_topItem == null) return;

      ScrollTo(_topItem);
    }

    private void ScrollTo(int index) {
      if (Items.Count - 1 < index || index < 0)
        return;

      _rowsStackPanel.BringIndexIntoViewPublic(index);

      // Scroll the row to top (the row will be scrolled to top in the LayoutUpdated event)
      _rowToScrollToTop = ItemContainerGenerator.ContainerFromIndex(index) as UIElement;
    }

    private void ScrollTo(object item) {
      if (item == null) return;

      ScrollTo(GetRowIndex(item));
    }

    private int GetRowIndex(object item) {
      var rowIndex = 0;
      var found = false;

      if (item is ItemsGroup) {
        rowIndex = Items.IndexOf(item);
        found = rowIndex != -1;
      }
      else
        foreach (var row in Items) {
          if (row is VirtualizingWrapPanelRow itemsRow && itemsRow.Items.Any(x => x.Equals(item))) {
            found = true;
            break;
          }
          rowIndex++;
        }

      return found ? rowIndex : 0;
    }

    private int GetRowIndex(FrameworkElement element) {
      foreach (var row in _rowsStackPanel.Children.Cast<DependencyObject>()) {
        if (element.IsDescendantOf(row))
          return ItemContainerGenerator.IndexFromContainer(row);
      }

      return 0;
    }

    private int GetTopRowIndex() {
      var rowIndex = 0;
      VisualTreeHelper.HitTest(this, null, (e) => {
        if (e.VisualHit is FrameworkElement elm) {
          rowIndex = GetRowIndex(elm);
          return HitTestResultBehavior.Stop;
        }
        return HitTestResultBehavior.Continue;
      }, new PointHitTestParameters(new(10, 40)));

      return rowIndex;
    }
  }
}
