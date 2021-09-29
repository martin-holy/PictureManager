using PictureManager.Domain.Models;
using PictureManager.Utils;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class SegmentsDrawerControl : UserControl {
    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value

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
      var changed = false;
      foreach (var segment in data as Segment[] ?? new Segment[] { data as Segment }) {
        if (App.Core.Segments.SegmentsDrawerToggle(segment))
          changed = true;
      }
      if (changed) _ = ReloadSegments();
    }

    public async Task ReloadSegments() {
      SegmentsGrid.ClearRows();
      UpdateLayout();
      SegmentsGrid.UpdateMaxRowWidth();

      foreach (var segment in App.Core.Segments.SegmentsDrawer) {
        await segment.SetPictureAsync(App.Core.Segments.SegmentSize);
        segment.MediaItem.SetThumbSize();
        segment.MediaItem.SetInfoBox();
        SegmentsGrid.AddItem(segment, _segmentGridWidth);
      }

      SegmentsGrid.ScrollToTop();
    }

    private void OnSegmentSelected(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var segment = (Segment)((FrameworkElement)sender).DataContext;
      App.Core.Segments.Select(App.Core.Segments.SegmentsDrawer, segment, isCtrlOn, isShiftOn);
    }
  }
}
