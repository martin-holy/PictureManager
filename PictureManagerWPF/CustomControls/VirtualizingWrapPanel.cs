using PictureManager.Domain;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PictureManager.CustomControls {
  public class VirtualizingWrapPanel : Control {

    private ScrollViewer _rowsScrollViewer;
    private VirtualizingStackPanel _rowsStackPanel;
    private double _maxRowWidth;

    public static readonly DependencyProperty ItemDataTemplateProperty = DependencyProperty.Register(nameof(ItemDataTemplate), typeof(DataTemplate), typeof(VirtualizingWrapPanel));
    public static readonly DependencyProperty ShowGroupItemsCountProperty = DependencyProperty.Register(nameof(ShowGroupItemsCount), typeof(bool), typeof(VirtualizingWrapPanel));
    public static readonly DependencyProperty GroupItemsCountIconProperty = DependencyProperty.Register(nameof(GroupItemsCountIcon), typeof(IconName), typeof(VirtualizingWrapPanel));
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(ObservableCollection<object>), typeof(VirtualizingWrapPanel));

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

    public ObservableCollection<object> Rows {
      get => (ObservableCollection<object>)GetValue(RowsProperty);
      set => SetValue(RowsProperty, value);
    }

    static VirtualizingWrapPanel() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(typeof(VirtualizingWrapPanel)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      var grid = (ItemsControl)Template.FindName("PART_Grid", this);
      grid.ApplyTemplate();
      grid.SizeChanged += (o, e) => { _maxRowWidth = ActualWidth; };

      var itemsPresenter = (ItemsPresenter)grid.Template.FindName("PART_ItemsPresenter", grid);
      itemsPresenter.ApplyTemplate();

      _rowsStackPanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as VirtualizingStackPanel;
      _rowsScrollViewer = (ScrollViewer)grid.Template.FindName("PART_RowsScrollViewer", grid);
    }

    public void ScrollTo(int index) => _rowsStackPanel.BringIndexIntoViewPublic(index);

    public void ScrollTo(object item) {
      var rowIndex = 0;
      foreach (var row in Rows) {
        if (row is VirtualizingWrapPanelRow itemsRow)
          if (itemsRow.Items.Any(x => x.Equals(item)))
            break;
        rowIndex++;
      }

      ScrollTo(rowIndex);
    }

    public void ScrollToTop() => _rowsScrollViewer?.ScrollToTop();

    public void ClearRows() {
      foreach (var row in Rows) {
        switch (row) {
          case VirtualizingWrapPanelRow r: r.Items.Clear(); break;
          case VirtualizingWrapPanelGroup g: g.Items.Clear(); break;
        }
      }

      Rows.Clear();
    }

    public void AddItem(object item, int itemWidth, VirtualizingWrapPanelGroupItem[] groupItems) {
      AddGroup(groupItems);

      var row = Rows.OfType<VirtualizingWrapPanelRow>().LastOrDefault();
      if (row == null || (row.Items.Count > 0 && itemWidth > row.SpaceLeft)) {
        row = new VirtualizingWrapPanelRow(_maxRowWidth);
        Rows.Add(row);
      }

      row.Items.Add(item);
      row.SpaceLeft -= itemWidth;
    }

    private void AddGroup(VirtualizingWrapPanelGroupItem[] groupItems) {
      var group = Rows.OfType<VirtualizingWrapPanelGroup>().LastOrDefault();
      if (group == null || !GroupItemsEquals(group.Items.ToArray(), groupItems)) {
        group = new VirtualizingWrapPanelGroup();

        foreach (var groupItem in groupItems)
          group.Items.Add(groupItem);

        Rows.Add(group);
        Rows.Add(new VirtualizingWrapPanelRow(_maxRowWidth));
      }

      group.ItemsCount++;
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
    public ObservableCollection<object> Items { get; } = new();
    public double SpaceLeft { get; set; }
    public VirtualizingWrapPanelRow(double maxWidth) {
      SpaceLeft = maxWidth;
    }
  }

  public class VirtualizingWrapPanelGroup : ObservableObject {
    private int _itemsCount;

    public int ItemsCount { get => _itemsCount; set { _itemsCount = value; OnPropertyChanged(); } }
    public ObservableCollection<VirtualizingWrapPanelGroupItem> Items { get; } = new();
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
