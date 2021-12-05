using PictureManager.Domain.Models;
using PictureManager.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MH.UI.WPF.Converters;
using PictureManager.Views;

namespace PictureManager.UserControls {
  public partial class SegmentsDrawerControl {
    public SegmentsDrawerControl() {
      InitializeComponent();

      DragDropFactory.SetDrag(SegmentsGrid, CanDrag);
      DragDropFactory.SetDrop(SegmentsGrid, CanDrop, DoDrop);
    }

    private object CanDrag(MouseEventArgs e) =>
      (e.OriginalSource as FrameworkElement)?.DataContext is SegmentV segmentV ? App.Core.SegmentsM.GetOneOrSelected(segmentV.Segment) : null;

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      if (!SegmentsGrid.Equals(source) && !App.Core.SegmentsM.SegmentsDrawer.Contains(data))
        return DragDropEffects.Copy;
      if (SegmentsGrid.Equals(source) && (data as SegmentM[])?.Contains(((e.OriginalSource as FrameworkElement)?.DataContext as SegmentV)?.Segment) == false)
        return DragDropEffects.Move;
      return DragDropEffects.None;
    }

    private void DoDrop(DragEventArgs e, object source, object data) {
      foreach (var segment in data as SegmentM[] ?? new[] { (data as SegmentV)?.Segment })
        App.Core.SegmentsM.SegmentsDrawerToggle(segment);
    }

    private void OnSegmentSelected(object o, ClickEventArgs e) {
      if (e.DataContext is SegmentV segmentV)
        App.Core.SegmentsM.Select(App.Core.SegmentsM.SegmentsDrawer.ToList(), segmentV.Segment, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
