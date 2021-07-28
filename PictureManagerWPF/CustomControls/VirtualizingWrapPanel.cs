using PictureManager.Domain;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PictureManager.CustomControls {
  public class VirtualizingWrapPanel : Control {
    private ItemsControl _grid;
    private UIElement _rowToScrollToTop;
    private ScrollViewer _rowsScrollViewer;
    private VirtualizingStackPanel _rowsStackPanel;
    private VirtualizingWrapPanelGroup _lastGroup;
    private VirtualizingWrapPanelRow _lastRow;
    private double _maxRowWidth;

    public ObservableCollection<object> Rows { get; } = new();

    public static readonly DependencyProperty ItemDataTemplateProperty = DependencyProperty.Register(nameof(ItemDataTemplate), typeof(DataTemplate), typeof(VirtualizingWrapPanel));
    public static readonly DependencyProperty ShowGroupItemsCountProperty = DependencyProperty.Register(nameof(ShowGroupItemsCount), typeof(bool), typeof(VirtualizingWrapPanel));
    public static readonly DependencyProperty GroupItemsCountIconProperty = DependencyProperty.Register(nameof(GroupItemsCountIcon), typeof(IconName), typeof(VirtualizingWrapPanel));
    public static readonly DependencyProperty ScrollViewerSpeedFactorProperty = DependencyProperty.Register(nameof(ScrollViewerSpeedFactor), typeof(double), typeof(VirtualizingWrapPanel), new PropertyMetadata(2.5));

    public DataTemplate ItemDataTemplate {
      get => (DataTemplate)GetValue(ItemDataTemplateProperty);
      set => SetValue(ItemDataTemplateProperty, value);
    }

    public bool ShowGroupItemsCount {
      get => (bool)GetValue(ShowGroupItemsCountProperty);
      set => SetValue(ShowGroupItemsCountProperty, value);
    }

    public IconName GroupItemsCountIcon {
      get => (IconName)GetValue(GroupItemsCountIconProperty);
      set => SetValue(GroupItemsCountIconProperty, value);
    }

    public double ScrollViewerSpeedFactor {
      get => (double)GetValue(ScrollViewerSpeedFactorProperty);
      set => SetValue(ScrollViewerSpeedFactorProperty, value);
    }

    static VirtualizingWrapPanel() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(typeof(VirtualizingWrapPanel)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      _grid = (ItemsControl)Template.FindName("PART_Grid", this);
      _ = _grid.ApplyTemplate();
      _grid.SizeChanged += (o, e) => _maxRowWidth = ActualWidth;

      var itemsPresenter = (ItemsPresenter)_grid.Template.FindName("PART_ItemsPresenter", _grid);
      _ = itemsPresenter.ApplyTemplate();

      _rowsStackPanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as VirtualizingStackPanel;
      _rowsScrollViewer = (ScrollViewer)_grid.Template.FindName("PART_RowsScrollViewer", _grid);

      LayoutUpdated += (o, e) => {
        if (_rowToScrollToTop == null) return;
        _rowsScrollViewer?.ScrollToVerticalOffset(_rowsStackPanel.GetItemOffset(_rowToScrollToTop));
        _rowToScrollToTop = null;
      };
    }

    public void ScrollTo(int index) {
      if (Rows.Count - 1 < index) return;

      _rowsStackPanel.BringIndexIntoViewPublic(index);

      // Scroll the row to top (the row will be scrolled to top in the LayoutUpdated event)
      _rowToScrollToTop = _grid.ItemContainerGenerator.ContainerFromIndex(index) as UIElement;
    }

    public void ScrollTo(object item) => ScrollTo(GetRowIndex(item));

    public int GetRowIndex(object item) {
      var rowIndex = 0;
      foreach (var row in Rows) {
        if (row is VirtualizingWrapPanelRow itemsRow && itemsRow.Items.Any(x => x.Equals(item)))
          break;
        rowIndex++;
      }

      return rowIndex;
    }

    public int GetRowIndex(FrameworkElement element) {
      foreach (var row in _rowsStackPanel.Children.Cast<DependencyObject>()) {
        if (element.IsDescendantOf(row))
          return _grid.ItemContainerGenerator.IndexFromContainer(row);
      }

      return 0;
    }

    public void ScrollToTop() => _rowsScrollViewer?.ScrollToTop();

    public void UpdateMaxRowWidth() => _maxRowWidth = ActualWidth;

    public void ClearRows() {
      foreach (var row in Rows) {
        switch (row) {
          case VirtualizingWrapPanelRow r:
          r.Items.Clear();
          break;

          case VirtualizingWrapPanelGroup g:
          g.GroupInfo.Clear();
          g.Items.Clear();
          break;
        }
      }

      Rows.Clear();
      _lastGroup = null;
      _lastRow = null;
    }

    public void AddGroup(IconName icon, string title) =>
      AddGroup(new VirtualizingWrapPanelGroupItem[] { new() { Icon = icon, Title = title } });

    public void AddGroup(VirtualizingWrapPanelGroupItem[] groupItems) {
      _lastGroup = new VirtualizingWrapPanelGroup();

      foreach (var groupItem in groupItems)
        _lastGroup.GroupInfo.Add(groupItem);

      Rows.Add(_lastGroup);
      _lastRow = null;
    }

    public void AddGroupIfNew(VirtualizingWrapPanelGroupItem[] groupItems) {
      if (_lastGroup == null || !GroupItemsEquals(_lastGroup.GroupInfo.ToArray(), groupItems))
        AddGroup(groupItems);
    }

    public void AddItem(object item, int itemWidth) {
      if (_lastRow == null || (_lastRow.Items.Count > 0 && itemWidth > _lastRow.SpaceLeft)) {
        _lastRow = new VirtualizingWrapPanelRow(_maxRowWidth, _lastGroup);
        Rows.Add(_lastRow);
      }

      _lastGroup?.Items.Add(item);
      _lastRow.Items.Add(item);
      _lastRow.SpaceLeft -= itemWidth;

      if (_lastGroup != null)
        _lastGroup.ItemsCount++;
    }

    private static bool GroupItemsEquals(VirtualizingWrapPanelGroupItem[] gis1, VirtualizingWrapPanelGroupItem[] gis2) {
      if (gis1.Length != gis2.Length) return false;

      for (var i = 0; i < gis1.Length; i++) {
        var gi1 = gis1[i];
        var gi2 = gis2[i];
        if (gi1.Icon != gi2.Icon || gi1.ToolTip != gi2.ToolTip || gi1.Title != gi2.Title) return false;
      }

      return true;
    }
  }

  public class VirtualizingWrapPanelRow {
    public VirtualizingWrapPanelGroup Group { get; }
    public ObservableCollection<object> Items { get; } = new();
    public double SpaceLeft { get; set; }
    public VirtualizingWrapPanelRow(double maxWidth, VirtualizingWrapPanelGroup group) {
      Group = group;
      SpaceLeft = maxWidth;
    }
  }

  public class VirtualizingWrapPanelGroup : ObservableObject {
    private int _itemsCount;

    public int ItemsCount { get => _itemsCount; set { _itemsCount = value; OnPropertyChanged(); } }
    public ObservableCollection<VirtualizingWrapPanelGroupItem> GroupInfo { get; } = new();
    public List<object> Items { get; } = new();
  }

  public class VirtualizingWrapPanelGroupItem : ObservableObject {
    private IconName _icon;
    private string _title;
    private string _toolTip;

    public IconName Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string ToolTip { get => _toolTip; set { _toolTip = value; OnPropertyChanged(); } }
  }

  public class ObservableObject : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
}
