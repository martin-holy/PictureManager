using System;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Utils {
  public static class DragDropFactory {
    public static void SetDrag(UIElement source, Func<FrameworkElement, object> canDrag) {
      source.MouseMove += (o, e) => {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        var dataSource = e.OriginalSource as FrameworkElement;
        var data = canDrag.Invoke(dataSource);
        if (data == null) return;
        _ = DragDrop.DoDragDrop(dataSource, new object[] { data, source }, DragDropEffects.All);
      };
    }

    public static void SetDrop(UIElement target, Func<object, object, FrameworkElement, DragDropEffects> canDrop, Action<object> onDrop) {
      void AllowDropCheck(object sender, DragEventArgs e) {
        var (data, source) = GetData(e.Data);
        e.Effects = canDrop(source, data, e.OriginalSource as FrameworkElement);
        e.Handled = true;
      }

      target.AllowDrop = true;
      target.DragEnter += AllowDropCheck;
      target.DragLeave += AllowDropCheck;
      target.DragOver += AllowDropCheck;
      target.Drop += (o, e) => {
        var (data, src) = GetData(e.Data);
        if (data == null) return;
        onDrop.Invoke(data);
      };
    }

    public static (object, object) GetData(IDataObject dragData) =>
      dragData.GetData(typeof(object[])) is not object[] data || data.Length != 2 ? (null, null) : (data[0], data[1]);
  }
}
