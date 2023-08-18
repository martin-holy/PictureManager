using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Keyboard = System.Windows.Input.Keyboard;

namespace MH.UI.WPF.Controls {
  public class TreeViewBase : TreeView {
    public ScrollViewer ScrollViewer { get; set; }

    // TODO remove or replace with new one
    public static readonly DependencyProperty ScrollViewerSpeedFactorProperty = DependencyProperty.Register(
      nameof(ScrollViewerSpeedFactor), typeof(double), typeof(TreeViewBase), new(2.5));

    public static readonly DependencyProperty SelectActionProperty = DependencyProperty.Register(
      nameof(SelectAction), typeof(Action<object, bool, bool>), typeof(TreeViewBase));

    public double ScrollViewerSpeedFactor {
      get => (double)GetValue(ScrollViewerSpeedFactorProperty);
      set => SetValue(ScrollViewerSpeedFactorProperty, value);
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
    }

    private void SelectItem(MouseButtonEventArgs e) {
      if (e.Source is ToggleButton
          || (e.OriginalSource as FrameworkElement)
              ?.FindTopTemplatedParent() is not TreeViewItem tvi) return;

      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;

      SelectAction(tvi.DataContext, isCtrlOn, isShiftOn);
    }
  }
}
