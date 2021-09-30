using PictureManager.Domain.Models;
using PictureManager.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class SegmentsDrawerControl : UserControl {
    public SegmentsDrawerControl() {
      InitializeComponent();

      DragDropFactory.SetDrag(SegmentsGrid, CanDrag);
      DragDropFactory.SetDrop(SegmentsGrid, CanDrop, DoDrop);
    }

    private object CanDrag(MouseEventArgs e) =>
      (e.OriginalSource as FrameworkElement)?.DataContext is Segment segment ? App.Core.Segments.GetOneOrSelected(segment) : null;

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      if (source != SegmentsGrid && !App.Core.Segments.SegmentsDrawer.Contains(data))
        return DragDropEffects.Copy;
      if (source == SegmentsGrid && (data as Segment[])?.Contains((e.OriginalSource as FrameworkElement)?.DataContext) == false)
        return DragDropEffects.Move;
      return DragDropEffects.None;
    }

    private void DoDrop(DragEventArgs e, object source, object data) {
      foreach (var segment in data as Segment[] ?? new Segment[] { data as Segment })
        App.Core.Segments.SegmentsDrawerToggle(segment);
    }

    private void OnSegmentSelected(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var segment = (Segment)((FrameworkElement)sender).DataContext;
      App.Core.Segments.Select(App.Core.Segments.SegmentsDrawer.ToList(), segment, isCtrlOn, isShiftOn);
    }
  }
}
