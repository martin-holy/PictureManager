using MH.UI.Interfaces;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Keyboard = System.Windows.Input.Keyboard;

namespace MH.UI.WPF.Controls {
  public class CollectionView : TreeViewBase {
    private double _verticalOffset;
    private bool _isScrolling;

    public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
      nameof(View), typeof(ICollectionView), typeof(CollectionView));

    public ICollectionView View {
      get => (ICollectionView)GetValue(ViewProperty);
      set => SetValue(ViewProperty, value);
    }

    public RelayCommand<MouseButtonEventArgs> OpenItemCommand { get; }
    public new RelayCommand<MouseButtonEventArgs> SelectItemCommand { get; }

    static CollectionView() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(CollectionView),
        new FrameworkPropertyMetadata(typeof(CollectionView)));
    }

    public CollectionView() {
      OpenItemCommand = new(OpenItem);
      SelectItemCommand = new(SelectItem);
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      ScrollViewer.ScrollChanged += (_, e) => {
        if (!View.IsSizeChanging && !_isScrolling)
          SetTopItem();

        if (Math.Abs(e.VerticalOffset - _verticalOffset) > 0) return;
        _isScrolling = false;
        _verticalOffset = 0;
      };

      LayoutUpdated += (_, _) => {
        if (_verticalOffset > 0)
          ScrollViewer.ScrollToVerticalOffset(_verticalOffset);

        if (View.IsSizeChanging)
          View.IsSizeChanging = false;
      };

      SizeChanged += (_, e) => {
        if (e.PreviousSize is not { Width: 0, Height: 0 })
          View.IsSizeChanging = true;
      };

      // TODO expand all in one update, so maybe removing binding to IsSelected?
      // keep binding for double click 
      PreviewMouseLeftButtonUp += (_, e) => {
        if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0
            && e.OriginalSource is ToggleButton { Name: "Expander" } btn) {
          View.SetExpanded(btn.DataContext);
        }
      };

      ItemsSource = View.RootHolder;

      View.PropertyChanged += (_, e) => {
        switch (e.PropertyName) {
          case nameof(View.ScrollToItems): ScrollToItems(); break;
          case nameof(View.ScrollToTop): ScrollToTop(); break;
        }
      };
    }

    private static object GetDataContext(object source) =>
      ((source as FrameworkElement)?.Parent as FrameworkElement)?
      .FindTopTemplatedParent()?
      .DataContext;

    private void OpenItem(MouseButtonEventArgs e) {
      if (e.ChangedButton != MouseButton.Left) return;
      View.OpenItem(GetDataContext(e.OriginalSource));
    }

    private void SelectItem(MouseButtonEventArgs e) {
      var item = GetDataContext(e.OriginalSource);
      var row = (e.Source as FrameworkElement)?.DataContext;
      var btn = e.OriginalSource as Button ?? (e.OriginalSource as FrameworkElement)?.TryFindParent<Button>();

      if (item == null || row == null || btn != null) return;

      bool isCtrlOn;
      bool isShiftOn;

      if (e.ChangedButton is not MouseButton.Left) {
        isCtrlOn = true;
        isShiftOn = false;
      }
      else {
        isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
        isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      }

      View.SelectItem(row, item, isCtrlOn, isShiftOn);
    }

    private void SetTopItem() {
      VisualTreeHelper.HitTest(this, null, e => {
        if (e.VisualHit is FrameworkElement elm && View.SetTopItem(elm.DataContext))
          return HitTestResultBehavior.Stop;

        return HitTestResultBehavior.Continue;
      }, new PointHitTestParameters(new(10, 10)));
    }

    private void ScrollToItems() {
      if (View.ScrollToItems == null || !IsVisible || ScrollViewer == null) return;
      var items = View.ScrollToItems;
      View.ScrollToItems = null;
      _isScrolling = true;
      ScrollViewer.UpdateLayout();

      var parent = this as ItemsControl;
      foreach (var item in items) {
        var index = parent.Items.IndexOf(item);
        if (index < 0) break;
        var panel = parent.GetChildOfType<VirtualizingStackPanel>();
        if (panel == null) break;
        panel.BringIndexIntoViewPublic(index);
        if (parent.ItemContainerGenerator.ContainerFromIndex(index) is not TreeViewItem tvi) break;
        parent = tvi;
      }

      _verticalOffset = View.IsScrollUnitItem
        ? View.ScrollToIndex
        : ScrollViewer.VerticalOffset
          + parent.TransformToVisual(ScrollViewer).Transform(new(0, 0)).Y;
    }

    private void ScrollToTop() {
      if (!View.ScrollToTop) return;
      ScrollViewer?.ScrollToTop();
      ScrollViewer?.UpdateLayout();
      View.ScrollToTop = false;
    }
  }
}
