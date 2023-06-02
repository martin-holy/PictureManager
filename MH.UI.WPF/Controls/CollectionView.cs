﻿using MH.Utils.BaseClasses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MH.UI.WPF.Utils;
using MH.Utils;
using Keyboard = System.Windows.Input.Keyboard;

namespace MH.UI.WPF.Controls {
  public class CollectionView : TreeView {
    public ScrollViewer ScrollViewer { get; set; }

    public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
      nameof(View), typeof(MH.Utils.CollectionView), typeof(CollectionView));

    public static readonly DependencyProperty ScrollViewerSpeedFactorProperty = DependencyProperty.Register(
      nameof(ScrollViewerSpeedFactor), typeof(double), typeof(CollectionView), new(2.5));

    public MH.Utils.CollectionView View {
      get => (MH.Utils.CollectionView)GetValue(ViewProperty);
      set => SetValue(ViewProperty, value);
    }

    public double ScrollViewerSpeedFactor {
      get => (double)GetValue(ScrollViewerSpeedFactorProperty);
      set => SetValue(ScrollViewerSpeedFactorProperty, value);
    }

    public RelayCommand<MouseButtonEventArgs> SelectItemCommand { get; }

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

      ItemsSource = new[] { View.Root };
    }

    private void SelectItem(MouseButtonEventArgs e) {
      var item = (e.OriginalSource as FrameworkElement)?.FindTopTemplatedParent()?.DataContext;
      var row = (e.Source as FrameworkElement)?.DataContext as CollectionViewRow;

      if (item == null || row == null) return;

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

      View.Select(row.Group.Source, item, isCtrlOn, isShiftOn);
    }
  }
}
