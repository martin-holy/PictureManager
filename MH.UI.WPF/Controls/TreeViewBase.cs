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
  public class TreeViewBase : TreeView {
    private double _verticalOffset = -1;
    private bool _isScrolling;
    private int _scrollToAttempts;

    public ScrollViewer ScrollViewer { get; set; }

    public static readonly DependencyProperty TreeViewProperty = DependencyProperty.Register(
      nameof(TreeView), typeof(ITreeView), typeof(TreeViewBase));

    public static readonly DependencyProperty SelectActionProperty = DependencyProperty.Register(
      nameof(SelectAction), typeof(Action<object, bool, bool>), typeof(TreeViewBase));

    public ITreeView TreeView {
      get => (ITreeView)GetValue(TreeViewProperty);
      set => SetValue(TreeViewProperty, value);
    }

    public Action<object, bool, bool> SelectAction {
      get => (Action<object, bool, bool>)GetValue(SelectActionProperty);
      set => SetValue(SelectActionProperty, value);
    }

    public RelayCommand<MouseButtonEventArgs> SelectItemCommand { get; }

    public TreeViewBase() {
      SelectItemCommand = new(SelectItem);
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      ScrollViewer = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);

      ScrollViewer.ScrollChanged += (_, e) => {
        if (TreeView is { IsSizeChanging: false } && !_isScrolling)
          SetTopItem();

        if (Math.Abs(e.VerticalOffset - _verticalOffset) > 0) return;
        _isScrolling = false;
        _verticalOffset = -1;
      };

      if (TreeView != null) {
        ItemsSource = TreeView.RootHolder;

        TreeView.PropertyChanged += (_, e) => {
          switch (e.PropertyName) {
            case nameof(TreeView.ScrollToItems): ScrollToItems(); break;
            case nameof(TreeView.ScrollToTop): ScrollToTop(); break;
          }
        };
      }

      LayoutUpdated += (_, _) => {
        if (TreeView == null) return;

        if (_verticalOffset > -1) {
          ScrollViewer.ScrollToVerticalOffset(_verticalOffset);

          _scrollToAttempts--;
          if (_scrollToAttempts < 0) {
            _isScrolling = false;
            _verticalOffset = -1;
          }
        }

        if (TreeView.IsSizeChanging)
          TreeView.IsSizeChanging = false;
      };

      SizeChanged += (_, e) => {
        if (TreeView != null && e.PreviousSize is not { Width: 0, Height: 0 })
          TreeView.IsSizeChanging = true;
      };
    }

    private void SelectItem(MouseButtonEventArgs e) {
      if (e.Source is ToggleButton
          || (e.OriginalSource as FrameworkElement)
              ?.FindTopTemplatedParent() is not TreeViewItem tvi) return;

      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;

      SelectAction(tvi.DataContext, isCtrlOn, isShiftOn);
    }

    private void ScrollToItems() {
      if (TreeView.ScrollToItems == null || !IsVisible || ScrollViewer == null) return;
      var items = TreeView.ScrollToItems;
      TreeView.ScrollToItems = null;
      _isScrolling = true;
      ScrollViewer.UpdateLayout();

      ItemsControl parent = this;
      foreach (var item in items) {
        var index = parent.Items.IndexOf(item);
        if (index < 0) break;
        var panel = parent.GetChildOfType<VirtualizingStackPanel>();
        if (panel == null) break;
        panel.BringIndexIntoViewPublic(index);
        if (parent.ItemContainerGenerator.ContainerFromIndex(index) is not TreeViewItem tvi) break;
        parent = tvi;
      }

      _verticalOffset = TreeView.IsScrollUnitItem
        ? TreeView.ScrollToIndex
        : ScrollViewer.VerticalOffset
          + parent.TransformToVisual(ScrollViewer).Transform(new(0, 0)).Y;

      if (_verticalOffset > -1)
        _scrollToAttempts = 5;
      else
        _isScrolling = false;
    }

    private void ScrollToTop() {
      if (!TreeView.ScrollToTop) return;
      ScrollViewer?.ScrollToTop();
      ScrollViewer?.UpdateLayout();
      TreeView.ScrollToTop = false;
    }

    private void SetTopItem() {
      VisualTreeHelper.HitTest(this, null, e => {
        if (e.VisualHit is FrameworkElement elm && TreeView.SetTopItem(elm.DataContext))
          return HitTestResultBehavior.Stop;

        return HitTestResultBehavior.Continue;
      }, new PointHitTestParameters(new(10, 10)));
    }

    /// <summary>
    /// Scroll TreeView when the mouse is near the top or bottom
    /// </summary>
    public void DragDropAutoScroll(DragEventArgs e) {
      const int px = 25;
      var unit = TreeView.IsScrollUnitItem ? 1 : px;
      var pos = e.GetPosition(this);
      if (pos.Y < px)
        ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - unit);
      else if (ActualHeight - pos.Y < px)
        ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + unit);
    }
  }
}
