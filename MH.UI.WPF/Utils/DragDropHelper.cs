using System;
using System.Windows;
using System.Windows.Input;
using static MH.Utils.DragDropHelper;

namespace MH.UI.WPF.Utils {
  public static class DragDropHelper {
    private static Point _dragStartPosition;
    private static bool _isActive;
    private static object _sourceControl;
    private static object _data;

    public static DragEventArgs DragEventArgs { get; set; }

    public static readonly DependencyProperty IsDragEnabledProperty = DependencyProperty.RegisterAttached(
      "IsDragEnabled", typeof(bool), typeof(DragDropHelper), new(OnIsDragEnabledChanged));
    
    public static readonly DependencyProperty IsDropEnabledProperty = DependencyProperty.RegisterAttached(
      "IsDropEnabled", typeof(bool), typeof(DragDropHelper), new(OnIsDropEnabledChanged));
    
    public static readonly DependencyProperty DragDataTypeProperty = DependencyProperty.RegisterAttached(
      "DragDataType", typeof(Type), typeof(DragDropHelper));
    
    public static readonly DependencyProperty CanDragProperty = DependencyProperty.RegisterAttached(
      "CanDrag", typeof(CanDragFunc), typeof(DragDropHelper));
    
    public static readonly DependencyProperty CanDropProperty = DependencyProperty.RegisterAttached(
      "CanDrop", typeof(CanDropFunc), typeof(DragDropHelper));
    
    public static readonly DependencyProperty DoDropProperty = DependencyProperty.RegisterAttached(
      "DoDrop", typeof(DoDropAction), typeof(DragDropHelper));
    
    public static readonly DependencyProperty DataFormatProperty = DependencyProperty.RegisterAttached(
      "DataFormat", typeof(string), typeof(DragDropHelper));

    public static bool GetIsDragEnabled(DependencyObject d) => (bool)d.GetValue(IsDragEnabledProperty);
    public static void SetIsDragEnabled(DependencyObject d, bool value) => d.SetValue(IsDragEnabledProperty, value);
    public static bool GetIsDropEnabled(DependencyObject d) => (bool)d.GetValue(IsDropEnabledProperty);
    public static void SetIsDropEnabled(DependencyObject d, bool value) => d.SetValue(IsDropEnabledProperty, value);
    public static Type GetDragDataType(DependencyObject d) => (Type)d.GetValue(DragDataTypeProperty);
    public static void SetDragDataType(DependencyObject d, Type value) => d.SetValue(DragDataTypeProperty, value);
    public static CanDragFunc GetCanDrag(DependencyObject d) => (CanDragFunc)d.GetValue(CanDragProperty);
    public static void SetCanDrag(DependencyObject d, CanDragFunc value) => d.SetValue(CanDragProperty, value);
    public static CanDropFunc GetCanDrop(DependencyObject d) => (CanDropFunc)d.GetValue(CanDropProperty);
    public static void SetCanDrop(DependencyObject d, CanDropFunc value) => d.SetValue(CanDropProperty, value);
    public static DoDropAction GetDoDrop(DependencyObject d) => (DoDropAction)d.GetValue(DoDropProperty);
    public static void SetDoDrop(DependencyObject d, DoDropAction value) => d.SetValue(DoDropProperty, value);
    public static string GetDataFormat(DependencyObject d) => (string)d.GetValue(DataFormatProperty);
    public static void SetDataFormat(DependencyObject d, string value) => d.SetValue(DataFormatProperty, value);
    
    private static void OnIsDragEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not FrameworkElement fe)
        throw new InvalidOperationException();

      if ((bool)e.NewValue) {
        fe.PreviewMouseLeftButtonDown += StartDrag;
        fe.MouseMove += Draging;
      }
      else {
        fe.PreviewMouseLeftButtonDown -= StartDrag;
        fe.MouseMove -= Draging;
      }
    }

    private static void OnIsDropEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not FrameworkElement fe)
        throw new InvalidOperationException();

      if ((bool)e.NewValue) {
        fe.AllowDrop = true;
        fe.DragEnter += AllowDropCheck;
        fe.DragLeave += AllowDropCheck;
        fe.DragOver += AllowDropCheck;
        fe.Drop += Drop;
      }
      else {
        fe.AllowDrop = false;
        fe.DragEnter -= AllowDropCheck;
        fe.DragLeave -= AllowDropCheck;
        fe.DragOver -= AllowDropCheck;
        fe.Drop -= Drop;
      }
    }

    private static void StartDrag(object sender, MouseButtonEventArgs e) {
      _dragStartPosition = e.GetPosition((IInputElement)sender);
      _isActive = true;
    }

    private static void Draging(object sender, MouseEventArgs e) {
      if (!_isActive || e.LeftButton != MouseButtonState.Pressed
        || !HasDragStarted(e) || sender is not DependencyObject d) return;

      var dragData = (e.OriginalSource as FrameworkElement)?.DataContext;
      var dragDataType = GetDragDataType(d);
      var canDrag = GetCanDrag(d);

      var data = dragData == null
        ? null
        : canDrag != null
          ? canDrag(dragData)
          : dragDataType == null
            ? dragData
            : dragDataType.Equals(dragData.GetType())
              ? dragData
              : null;

      if (data == null) {
        _isActive = false;
        _sourceControl = null;
        _data = null;
        return;
      }

      _sourceControl = sender;
      _data = data;
      var dataFormat = GetDataFormat(d);
      var dataObject = string.IsNullOrEmpty(dataFormat)
        ? data
        : new DataObject(dataFormat, data);
      _ = DragDrop.DoDragDrop(d, dataObject, DragDropEffects.All);
    }

    private static bool HasDragStarted(MouseEventArgs e) {
      var diff = _dragStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    private static void AllowDropCheck(object sender, DragEventArgs e) {
      DragEventArgs = e;
      var target = (e.OriginalSource as FrameworkElement)?.DataContext;
      e.Effects = (DragDropEffects)GetCanDrop((DependencyObject)sender)(target, _data, sender.Equals(_sourceControl));
      e.Handled = true;
    }

    private static void Drop(object sender, DragEventArgs e) {
      if (_data == null) return;
      GetDoDrop((DependencyObject)sender)(_data, sender.Equals(_sourceControl));
    }
  }
}
