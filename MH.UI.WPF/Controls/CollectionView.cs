using MH.UI.Interfaces;
using MH.UI.WPF.Utils;
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

    public RelayCommand<MouseButtonEventArgs> OpenItemCommand { get; }
    public RelayCommand<MouseButtonEventArgs> SelectItemCommand { get; }

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

    private void OpenItem(MouseButtonEventArgs e) {
      if (!View.CanOpen || e.ChangedButton != MouseButton.Left) return;
      View.OpenItem(GetDataContext(e.OriginalSource));
    }

    private void SelectItem(MouseButtonEventArgs e) {
      if (!View.CanSelect) return;
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
