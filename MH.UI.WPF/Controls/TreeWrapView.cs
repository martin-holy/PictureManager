using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class TreeWrapRow {
    public ObservableCollection<object> Items { get; } = new();
    public bool IsExpanded { get; set; }
  }

  public class TreeWrapView : TreeView {
    private double _verticalOffset;
    private object _topItem;
    private object _topItemBeforeReload;
    private readonly Dictionary<ObservableCollection<object>, TreeWrapGroup> _treeWrapGroups = new();

    public ScrollViewer ScrollViewer;
    public event EventHandler WidthChangedEventHandler = delegate { };

    #region DependencyProperties
    public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
      nameof(ItemWidth), typeof(double), typeof(TreeWrapView));

    public static readonly DependencyProperty ItemWidthGetterProperty = DependencyProperty.Register(
      nameof(ItemWidthGetter), typeof(Func<object, int>), typeof(TreeWrapView));

    public static readonly DependencyProperty RootProperty = DependencyProperty.Register(
      nameof(Root), typeof(TreeWrapGroup), typeof(TreeWrapView), new(RootChanged));

    public static readonly DependencyProperty ScrollToItemProperty = DependencyProperty.Register(
      nameof(ScrollToItem), typeof(object), typeof(TreeWrapView), new(ScrollToItemChanged));

    public static readonly DependencyProperty ShowRootProperty = DependencyProperty.Register(
      nameof(ShowRoot), typeof(bool), typeof(TreeWrapView));

    public static readonly DependencyProperty ScrollViewerSpeedFactorProperty = DependencyProperty.Register(
      nameof(ScrollViewerSpeedFactor), typeof(double), typeof(TreeWrapView), new(2.5));

    public static readonly DependencyProperty ReloadAutoScrollProperty = DependencyProperty.Register(
      nameof(ReloadAutoScroll), typeof(bool), typeof(TreeWrapView), new(true));

    public static readonly DependencyProperty ReWrapItemsProperty = DependencyProperty.Register(
      nameof(ReWrapItems), typeof(bool), typeof(TreeWrapView),
      new((o, e) => {
        if ((bool)e.NewValue && o is TreeWrapView self) {
          self.ReWrapItems = false;
          self.ReWrap();
        }
      }));

    public double ItemWidth {
      get => (double)GetValue(ItemWidthProperty);
      set => SetValue(ItemWidthProperty, value);
    }

    public Func<object, int> ItemWidthGetter {
      get => (Func<object, int>)GetValue(ItemWidthGetterProperty);
      set => SetValue(ItemWidthGetterProperty, value);
    }

    public TreeWrapGroup Root {
      get => (TreeWrapGroup)GetValue(RootProperty);
      set => SetValue(RootProperty, value);
    }

    public object ScrollToItem {
      get => GetValue(ScrollToItemProperty);
      set => SetValue(ScrollToItemProperty, value);
    }

    public bool ShowRoot {
      get => (bool)GetValue(ShowRootProperty);
      set => SetValue(ShowRootProperty, value);
    }

    public double ScrollViewerSpeedFactor {
      get => (double)GetValue(ScrollViewerSpeedFactorProperty);
      set => SetValue(ScrollViewerSpeedFactorProperty, value);
    }

    public bool ReloadAutoScroll {
      get => (bool)GetValue(ReloadAutoScrollProperty);
      set => SetValue(ReloadAutoScrollProperty, value);
    }

    public bool ReWrapItems {
      get => (bool)GetValue(ReWrapItemsProperty);
      set => SetValue(ReWrapItemsProperty, value);
    }
    #endregion

    static TreeWrapView() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(TreeWrapView),
        new FrameworkPropertyMetadata(typeof(TreeWrapView)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      ScrollViewer = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);

      ScrollViewer.ScrollChanged += (_, _) =>
        _topItem = GetTopItem();

      LayoutUpdated += (_, _) => {
        if (_topItemBeforeReload != null) {
          ScrollTo(_topItemBeforeReload);
          _topItemBeforeReload = null;
        }

        if (_verticalOffset > 0) {
          ScrollViewer.ScrollToVerticalOffset(_verticalOffset);
          _verticalOffset = 0;
        }
      };

      Loaded += (_, _) =>
        SetSource();

      SizeChanged += (_, e) => {
        if (e.WidthChanged)
          WidthChangedEventHandler(this, EventArgs.Empty);
      };
    }

    private object GetTopItem() {
      FrameworkElement topElement = null;

      VisualTreeHelper.HitTest(this, null, (e) => {
        if (e.VisualHit is FrameworkElement elm) {
          topElement = elm;
          return HitTestResultBehavior.Stop;
        }
        return HitTestResultBehavior.Continue;
      }, new PointHitTestParameters(new(10, 10)));

      var item = topElement?.DataContext;

      return item is TreeWrapRow row && row.Items.Count > 0
        ? row.Items[0]
        : item is TreeWrapGroup group && group.Items.Count > 0
          ? group.Items[0]
          : item;
    }

    private static void RootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
      ((TreeWrapView)d).OnRootChanged(e.OldValue as TreeWrapGroup, e.NewValue as TreeWrapGroup);

    public void OnRootChanged(TreeWrapGroup oldValue, TreeWrapGroup newValue) {
      if (ReloadAutoScroll)
        _topItemBeforeReload = _topItem;

      SetSource();

      if (oldValue != null)
        RemoveItemsToWrapCollectionChangedEventHandlers(oldValue);

      if (newValue != null)
        AddItemsToWrapCollectionChangedEventHandlers(newValue);
    }

    private void SetSource() {
      AddAll(Root);
      ItemsSource = ShowRoot
        ? new[] { Root }
        : Root?.WrappedItems;
    }

    public void AddItemsToWrapCollectionChangedEventHandlers(TreeWrapGroup root) {
      _treeWrapGroups.Add(root.Items, root);
      root.Items.CollectionChanged += ItemsToWrapCollectionChanged;
      foreach (var group in root.Items.OfType<TreeWrapGroup>())
        AddItemsToWrapCollectionChangedEventHandlers(group);
    }

    public void RemoveItemsToWrapCollectionChangedEventHandlers(TreeWrapGroup root) {
      _treeWrapGroups.Remove(root.Items);
      root.Items.CollectionChanged -= ItemsToWrapCollectionChanged;
      foreach (var group in root.Items.OfType<TreeWrapGroup>())
        RemoveItemsToWrapCollectionChangedEventHandlers(group);
    }

    private void ItemsToWrapCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      var root = _treeWrapGroups[(ObservableCollection<object>)sender];
      switch (e.Action) {
        case NotifyCollectionChangedAction.Add:
          if (e.NewItems != null)
            foreach (var item in e.NewItems)
              if (item is TreeWrapGroup group) {
                AddItemsToWrapCollectionChangedEventHandlers(group);
                root.WrappedItems.Add(group);
                AddAll(group);
              }
              else
                AddItem(root, item);
        break;

        case NotifyCollectionChangedAction.Reset:
          foreach (var group in root.WrappedItems.OfType<TreeWrapGroup>())
            RemoveItemsToWrapCollectionChangedEventHandlers(group);

          root.WrappedItems.Clear();
        break;

        default:
          root.WrappedItems.Clear();
          AddAll(root);
        break;
      }
    }

    private void AddItem(TreeWrapGroup root, object item) {
      TreeWrapRow row = null;

      if (root.WrappedItems.Count > 0)
        row = root.WrappedItems[^1] as TreeWrapRow;

      if (row == null) {
        row = new();
        root.WrappedItems.Add(row);
      }

      var usedSpace = ItemWidthGetter != null
        ? row.Items.Sum(ItemWidthGetter)
        : row.Items.Count * ItemWidth;

      var itemWidth = ItemWidthGetter?.Invoke(item) ?? ItemWidth;

      if (ActualWidth - usedSpace < itemWidth) {
        row = new();
        root.WrappedItems.Add(row);
      }

      row.Items.Add(item);
    }

    public void AddAll(TreeWrapGroup root) {
      if (ActualWidth == 0 || root == null) return;

      root.WrappedItems.Clear();

      foreach (var item in root.Items) {
        if (item is TreeWrapGroup group) {
          root.WrappedItems.Add(group);
          AddAll(group);
        }
        else
          AddItem(root, item);
      }
    }

    public void ReWrap() {
      if (ReloadAutoScroll)
        _topItemBeforeReload = _topItem;

      AddAll(Root);
      ScrollTo(_topItem);
    }

    private static void ScrollToItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not TreeWrapView twv) return;
      twv.ScrollTo(e.NewValue);
      // reset so that scroll to same item is possible
      twv.ScrollToItem = null;
    }

    private void ScrollTo(object item) {
      if (item == null) return;

      var items = new List<object>();
      if (FindItem(item, Root, items) && ShowRoot)
        items.Add(Root);
      items.Reverse();

      var offset = 0.0;
      var parent = this as ItemsControl;

      foreach (var treeItem in items) {
        var index = parent.Items.IndexOf(treeItem);
        if (index < 0) break;
        var panel = parent.GetChildOfType<VirtualizingStackPanel>();
        if (panel == null) break;
        panel.BringIndexIntoViewPublic(index);
        if (parent.ItemContainerGenerator.ContainerFromIndex(index) is not TreeViewItem tvi) break;

        if (treeItem is TreeWrapGroup) {
          tvi.IsExpanded = true;

          if (items[^1] is TreeWrapRow)
            offset += tvi.GetChildOfType<Border>().ActualHeight;
        }   

        parent = tvi;
        offset += panel.GetItemOffset(tvi);
      }

      _verticalOffset = offset;
      ScrollViewer?.ScrollToHorizontalOffset(0);
    }

    private static bool FindItem(object item, TreeWrapGroup root, List<object> result) {
      foreach (var row in root.WrappedItems.OfType<TreeWrapRow>())
        if (row.Items.Contains(item)) {
          // add row to result only if the searched item is not the first one in the group
          if (!root.Items[0].Equals(item))
            result.Add(row);

          return true;
        }

      var found = false;
      foreach (var group in root.Items.OfType<TreeWrapGroup>())
        if (FindItem(item, group, result)) {
          result.Add(group);
          found = true;
        }

      return found;
    }
  }
}
