using MH.Utils.HelperClasses;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class VirtualizingWrapPanel : ItemsControl {
    private UIElement _rowToScrollToTop;
    private ScrollViewer _rowsScrollViewer;
    private VirtualizingStackPanel _rowsStackPanel;
    private object _topItem;

    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
      nameof(ItemWidth),
      typeof(double),
      typeof(VirtualizingWrapPanel));

    public static readonly DependencyProperty ItemsToWrapProperty = DependencyProperty.Register(
      nameof(ItemsToWrap),
      typeof(IEnumerable<object>),
      typeof(VirtualizingWrapPanel),
      new(ItemsToWrapChanged));

    public static readonly DependencyProperty WrappedItemsProperty = DependencyProperty.Register(
      nameof(WrappedItems),
      typeof(ObservableCollection<object>),
      typeof(VirtualizingWrapPanel));

    public static readonly DependencyProperty ScrollViewerSpeedFactorProperty = DependencyProperty.Register(
      nameof(ScrollViewerSpeedFactor),
      typeof(double),
      typeof(VirtualizingWrapPanel),
      new(2.5));

    public double ItemWidth {
      get => (double)GetValue(ItemWidthProperty);
      set => SetValue(ItemWidthProperty, value);
    }

    public IEnumerable<object> ItemsToWrap {
      get => (IEnumerable<object>)GetValue(ItemsToWrapProperty);
      set => SetValue(ItemsToWrapProperty, value);
    }

    public ObservableCollection<object> WrappedItems {
      get => (ObservableCollection<object>)GetValue(WrappedItemsProperty);
      set => SetValue(WrappedItemsProperty, value);
    }

    public double ScrollViewerSpeedFactor {
      get => (double)GetValue(ScrollViewerSpeedFactorProperty);
      set => SetValue(ScrollViewerSpeedFactorProperty, value);
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
      _rowsScrollViewer = (ScrollViewer)Template.FindName("PART_RowsScrollViewer", this);

      LayoutUpdated += (_, _) => {
        if (_rowToScrollToTop == null) return;
        _rowsScrollViewer?.ScrollToVerticalOffset(_rowsStackPanel.GetItemOffset(_rowToScrollToTop));
        _rowToScrollToTop = null;
      };

      _rowsScrollViewer.ScrollChanged += (_, _) => {
        _topItem = null;

        if (Items.Count > 0) {
          var item = Items[GetTopRowIndex()];

          _topItem = item is VirtualizingWrapPanelRow row
            ? row.Items[0]
            : item;
        }
      };

      Loaded += (_, _) => {
        Wrap();
      };
    }

    private static void ItemsToWrapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
      (d as VirtualizingWrapPanel)?.Wrap();

    public void Wrap() {
      if (ActualWidth == 0 || ItemWidth == 0) return;
      Wrap((int)(ActualWidth / ItemWidth));
    }

    private void Wrap(int columns) {
      WrappedItems ??= new();
      WrappedItems.Clear();

      if (ItemsToWrap == null || !ItemsToWrap.Any()) return;

      var looseItems = new List<object>();
      foreach (var item in ItemsToWrap) {
        if (item is ItemsGroup group) {
          if (looseItems.Count != 0) {
            WrapItems(looseItems, columns);
            looseItems.Clear();
          }

          WrappedItems.Add(group);
          WrapItems(group.Items, columns);
        }
        else
          looseItems.Add(item);
      }

      if (looseItems.Count != 0) {
        WrapItems(looseItems, columns);
        looseItems.Clear();
      }

      ScrollTopRowOrItem();
    }

    private void WrapItems(List<object> items, int columns) {
      var counter = 0;
      var row = new VirtualizingWrapPanelRow();
      WrappedItems.Add(row);

      foreach (var item in items) {
        if (counter == columns) {
          counter = 0;
          row = new();
          WrappedItems.Add(row);
        }

        row.Items.Add(item);
        counter++;
      }
    }

    private void ScrollTopRowOrItem() {
      if (_topItem == null) return;

      if (_topItem is ItemsGroup group)
        ScrollTo(Items.IndexOf(group));
      else
        ScrollTo(_topItem);
    }

    private void ScrollTo(int index) {
      if (Items.Count - 1 < index || index < 0) return;

      _rowsStackPanel.BringIndexIntoViewPublic(index);

      // Scroll the row to top (the row will be scrolled to top in the LayoutUpdated event)
      _rowToScrollToTop = ItemContainerGenerator.ContainerFromIndex(index) as UIElement;
    }

    private void ScrollTo(object item) =>
      ScrollTo(GetRowIndex(item));

    private int GetRowIndex(object item) {
      var rowIndex = 0;
      var found = false;
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

  public class VirtualizingWrapPanelRow {
    public ObservableCollection<object> Items { get; } = new();
  }
}
