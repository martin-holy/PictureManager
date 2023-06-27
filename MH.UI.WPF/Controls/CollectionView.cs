using MH.UI.Interfaces;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Keyboard = System.Windows.Input.Keyboard;

namespace MH.UI.WPF.Controls {
  public class CollectionView : TreeViewBase {
    private double _verticalOffset;

    public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
      nameof(View), typeof(ICollectionView), typeof(CollectionView));

    public ICollectionView View {
      get => (ICollectionView)GetValue(ViewProperty);
      set => SetValue(ViewProperty, value);
    }

    public new RelayCommand<MouseButtonEventArgs> SelectItemCommand { get; }

    static CollectionView() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(CollectionView),
        new FrameworkPropertyMetadata(typeof(CollectionView)));
    }

    public CollectionView() {
      SelectItemCommand = new(SelectItem);
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      ScrollViewer.ScrollChanged += (_, _) => {
        SetTopItem();
      };

      LayoutUpdated += (_, _) => {
        if (_verticalOffset > 0) {
          ScrollViewer.ScrollToHorizontalOffset(0);
          ScrollViewer.ScrollToVerticalOffset(_verticalOffset);
          _verticalOffset = 0;
        }

        if (View.IsSizeChanging)
          View.IsSizeChanging = false;
      };

      SizeChanged += (_, e) => {
        if (e.WidthChanged)
          View.IsSizeChanging = true;
      };

      ItemsSource = new[] { View.ObjectRoot };

      View.PropertyChanged += (_, e) => {
        if (nameof(View.ScrollToItem).Equals(e.PropertyName) && View.ScrollToItem != null && IsVisible) {
          ScrollTo(View.ScrollToItem);
          View.ScrollToItem = null;
        }
      };
    }

    private void SelectItem(MouseButtonEventArgs e) {
      var item = (e.OriginalSource as FrameworkElement)?.FindTopTemplatedParent()?.DataContext;
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

      View.Select(row, item, isCtrlOn, isShiftOn);
    }

    private void SetTopItem() {
      VisualTreeHelper.HitTest(this, null, e => {
        if (e.VisualHit is FrameworkElement elm && View.SetTopItem(elm.DataContext))
          return HitTestResultBehavior.Stop;

        return HitTestResultBehavior.Continue;
      }, new PointHitTestParameters(new(10, 10)));
    }

    private void ScrollTo(List<object> items) {
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

      _verticalOffset = ScrollViewer.VerticalOffset
        + parent.TransformToVisual(ScrollViewer).Transform(new(0, 0)).Y;
    }
  }
}
