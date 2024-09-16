using MH.UI.Interfaces;
using MH.UI.WPF.Extensions;
using MH.Utils.BaseClasses;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Keyboard = System.Windows.Input.Keyboard;

namespace MH.UI.WPF.Controls;

public class CollectionView : TreeViewBase {
  private double _openTime;
  private DateTime _lastClickTime = DateTime.Now;

  public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
    nameof(View), typeof(ICollectionView), typeof(CollectionView), new(_viewChanged));

  public ICollectionView? View {
    get => (ICollectionView?)GetValue(ViewProperty);
    set => SetValue(ViewProperty, value);
  }

  public static GroupByDialogDataTemplateSelector GroupByDialogDataTemplateSelector { get; } = new();

  public static RelayCommand<MouseButtonEventArgs> OpenItemCommand { get; } = new(_openItem);
  public static RelayCommand<MouseButtonEventArgs> SelectItemCommand { get; } = new(_selectItem);

  public override void OnApplyTemplate() {
    base.OnApplyTemplate();

    // TODO expand all in one update, so maybe removing binding to IsSelected?
    // keep binding for double click 
    PreviewMouseLeftButtonUp += (_, e) => {
      if ((Keyboard.Modifiers & ModifierKeys.Shift) > 0
          && e.OriginalSource is ToggleButton { Name: "expander" } btn) {
        View?.SetExpanded(btn.DataContext);
      }
    };
  }

  private static void _openItem(MouseButtonEventArgs? e) {
    if (e is not { ChangedButton: MouseButton.Left }
        || (e.OriginalSource as FrameworkElement)?.TryFindParent<CollectionView>() is not { View.CanOpen: true } cv) return;

    cv._openItem(_getDataContext(e.OriginalSource));
  }

  private void _openItem(object? item) {
    var startTime = DateTime.Now;
    View?.OpenItem(item);
    _openTime = (DateTime.Now - startTime).TotalMilliseconds;
  }

  private static void _selectItem(MouseButtonEventArgs? e) {
    if ((e?.OriginalSource as FrameworkElement)?.TryFindParent<CollectionView>() is not { View.CanSelect: true } cv
        || cv._doubleClicking()) return;

    var item = _getDataContext(e.OriginalSource);
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

  private bool _doubleClicking() {
    var sinceLastClick = (DateTime.Now - _lastClickTime).TotalMilliseconds;
    _lastClickTime = DateTime.Now;
    return sinceLastClick - _openTime < 300;
  }

  private static void _viewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is not CollectionView view) return;
    view.TreeView = view.View;
    if (view.View != null)
      view.View.UIView = view;
  }

  private static object? _getDataContext(object source) =>
    source is not FrameworkElement fe
      ? null
      : (fe.TemplatedParent == null
        ? fe.Parent as FrameworkElement
        : fe).FindTopTemplatedParent()?.DataContext;
}

public class GroupByDialogDataTemplateSelector : DataTemplateSelector {
  public static Func<object, string?>? TypeToKey { get; set; }

  public override DataTemplate? SelectTemplate(object? item, DependencyObject container) {
    if (item == null || TypeToKey == null)
      return base.SelectTemplate(item, container);

    var key = TypeToKey(item);
    return key != null && Application.Current.TryFindResource(key) is DataTemplate template
      ? template
      : base.SelectTemplate(item, container);
  }
}