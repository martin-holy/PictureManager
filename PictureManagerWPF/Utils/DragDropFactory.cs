using System;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Utils {
  public static class DragDropFactory {
    private static Point _dragDropStartPosition;
    private static bool _isActive;
    private static object _source;
    private static object _data;

    public static void SetDrag(FrameworkElement source, Func<MouseEventArgs, object> canDrag, string dataFormat = null) {
      source.PreviewMouseLeftButtonDown += (_, e) => {
        _dragDropStartPosition = e.GetPosition(source);
        _isActive = true;
      };

      source.MouseMove += (o, e) => {
        if (!_isActive || e.LeftButton != MouseButtonState.Pressed || !HasDragStarted(e)) return;
        
        var data = canDrag.Invoke(e);
        
        if (data == null) {
          _isActive = false;
          _source = null;
          _data = null;
          return;
        }

        _source = source;
        _data = data;
        var dataObject = string.IsNullOrEmpty(dataFormat) ? data : new DataObject(dataFormat, data);
        _ = DragDrop.DoDragDrop(source, dataObject, DragDropEffects.All);
      };
    }

    public static void SetDrop(FrameworkElement target, Func<DragEventArgs, object, object, DragDropEffects> canDrop, Action<DragEventArgs, object, object> doDrop) {
      void AllowDropCheck(object sender, DragEventArgs e) {
        e.Effects = canDrop(e, _source, _data);
        e.Handled = true;
      }

      target.AllowDrop = true;
      target.DragEnter += AllowDropCheck;
      target.DragLeave += AllowDropCheck;
      target.DragOver += AllowDropCheck;
      target.Drop += (_, e) => {
        if (_data == null) return;
        doDrop.Invoke(e, _source, _data);
      };
    }

    private static bool HasDragStarted(MouseEventArgs e) {
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }
  }
}
