﻿using MH.UI.Interfaces;
using MH.UI.WPF.Extensions;
using MH.Utils.BaseClasses;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Keyboard = System.Windows.Input.Keyboard;

namespace MH.UI.WPF.Controls {
  public class CollectionView : TreeViewBase {
    public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
      nameof(View), typeof(ICollectionView), typeof(CollectionView), new(ViewChanged));

    private static void ViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not CollectionView view) return;
      view.TreeView = view.View;
    }

    public ICollectionView View {
      get => (ICollectionView)GetValue(ViewProperty);
      set => SetValue(ViewProperty, value);
    }

    public static GroupByDialogDataTemplateSelector GroupByDialogDataTemplateSelector { get; } = new();

    public static RelayCommand<MouseButtonEventArgs> OpenItemCommand { get; } = new(OpenItem);
    public static RelayCommand<MouseButtonEventArgs> SelectItemCommand { get; } = new(SelectItem);

    static CollectionView() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(CollectionView),
        new FrameworkPropertyMetadata(typeof(CollectionView)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      // TODO expand all in one update, so maybe removing binding to IsSelected?
      // keep binding for double click 
      PreviewMouseLeftButtonUp += (_, e) => {
        if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0
            && e.OriginalSource is ToggleButton { Name: "expander" } btn) {
          View.SetExpanded(btn.DataContext);
        }
      };
    }

    private static object GetDataContext(object source) {
      if (source is not FrameworkElement fe) return null;
      if (fe.TemplatedParent == null)
        fe = fe.Parent as FrameworkElement;

      return fe?.FindTopTemplatedParent()?.DataContext;
    }

    private static void OpenItem(MouseButtonEventArgs e) {
      if (e.ChangedButton != MouseButton.Left
          || (e.OriginalSource as FrameworkElement)?.TryFindParent<CollectionView>() is not { } cv
          || !cv.View.CanOpen) return;

      cv.View.OpenItem(GetDataContext(e.OriginalSource));
    }

    private static void SelectItem(MouseButtonEventArgs e) {
      if ((e.OriginalSource as FrameworkElement)?.TryFindParent<CollectionView>() is not { } cv
          || !cv.View.CanSelect) return;

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

      cv.View.SelectItem(row, item, isCtrlOn, isShiftOn);
    }
  }

  public class GroupByDialogDataTemplateSelector : DataTemplateSelector {
    public static Func<object, string> TypeToKey { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container) {
      if (item == null || TypeToKey == null)
        return base.SelectTemplate(item, container);

      var key = TypeToKey(item);
      return key != null && Application.Current.TryFindResource(key) is DataTemplate template
        ? template
        : base.SelectTemplate(item, container);
    }
  }
}
