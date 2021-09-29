using System;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Utils {
  public static class DragDropFactory {
    private static Point _dragDropStartPosition;
    private static bool _isActive;

    public static void SetDrag(FrameworkElement source, Func<MouseEventArgs, object> canDrag) {
      source.PreviewMouseLeftButtonDown += (o, e) => {
        _dragDropStartPosition = e.GetPosition(source);
        _isActive = true;
      };

      source.MouseMove += (o, e) => {
        if (!_isActive || e.LeftButton != MouseButtonState.Pressed || !HasDragStarted(e)) return;
        var data = canDrag.Invoke(e);
        if (data == null) {
          _isActive = false;
          return;
        }
        _ = DragDrop.DoDragDrop(source, new object[] { data, source }, DragDropEffects.All);
      };
    }

    public static void SetDrop(FrameworkElement target, Func<DragEventArgs, object, object, DragDropEffects> canDrop, Action<DragEventArgs, object, object> doDrop) {
      void AllowDropCheck(object sender, DragEventArgs e) {
        var (data, source) = GetData(e.Data);
        e.Effects = canDrop(e, source, data);
        e.Handled = true;
      }

      target.AllowDrop = true;
      target.DragEnter += AllowDropCheck;
      target.DragLeave += AllowDropCheck;
      target.DragOver += AllowDropCheck;
      target.Drop += (o, e) => {
        var (data, source) = GetData(e.Data);
        if (data == null) return;
        doDrop.Invoke(e, source, data);
      };
    }

    public static (object, object) GetData(IDataObject dragData) =>
      dragData.GetData(typeof(object[])) is not object[] data || data.Length != 2 ? (null, null) : (data[0], data[1]);

    public static bool HasDragStarted(MouseEventArgs e) {
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }
  }
}
