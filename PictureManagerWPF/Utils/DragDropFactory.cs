using System;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Utils {
  public class DragDropFactory {
    private Point _dragStartPosition;
    private readonly Func<FrameworkElement, bool> _canDrag;
    private readonly Func<FrameworkElement, (object, DragDropEffects)> _getDragData;
    private readonly Func<DragEventArgs, object, bool> _canDrop;
    private readonly Action<DragEventArgs, object> _onDrop;
    private readonly UIElement _srcElement;

    public DragDropFactory(UIElement src, UIElement target, Func<FrameworkElement, bool> canDrag, Func<FrameworkElement, (object, DragDropEffects)> getDragData, Func<DragEventArgs, object, bool> canDrop, Action<DragEventArgs, object> onDrop) {
      // source
      _srcElement = src;
      _srcElement.PreviewMouseLeftButtonDown += AddEvents;
      _canDrag = canDrag;
      _getDragData = getDragData;

      // target
      target.AllowDrop = true;
      target.DragEnter += AllowDropCheck;
      target.DragLeave += AllowDropCheck;
      target.DragOver += AllowDropCheck;
      target.Drop += OnDrop;
      _canDrop = canDrop;
      _onDrop = onDrop;
    }

    private void AddEvents(object sender, MouseButtonEventArgs e) {
      _dragStartPosition = e.GetPosition(null);
      _srcElement.MouseMove += DragInit;
      _srcElement.PreviewMouseLeftButtonUp += RemoveEvents;
    }

    private void RemoveEvents(object sender, MouseButtonEventArgs e) {
      _srcElement.MouseMove -= DragInit;
      _srcElement.PreviewMouseLeftButtonUp -= RemoveEvents;
    }

    private void DragInit(object sender, MouseEventArgs e) {
      if (HasDragStarted(e)) {
        RemoveEvents(null, null);

        var src = e.OriginalSource as FrameworkElement;
        if (_canDrag.Invoke(src)) {
          var (data, effects) = _getDragData.Invoke(src);
          _ = DragDrop.DoDragDrop(src, new[] { data, this }, effects);
        }
      }
    }

    private bool HasDragStarted(MouseEventArgs e) {
      var diff = _dragStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    private void AllowDropCheck(object sender, DragEventArgs e) {
      var data = GetData(e.Data);
      if (data == null || _canDrop(e, data)) return;

      // can't be dropped
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e) {
      var data = GetData(e.Data);
      if (data == null) return;
      _onDrop.Invoke(e, data);
    }

    private object GetData(IDataObject dragData) =>
      dragData.GetData(typeof(object[])) is not object[] data || data.Length != 2 || data[1] != this ? null : data[0];
  }
}
