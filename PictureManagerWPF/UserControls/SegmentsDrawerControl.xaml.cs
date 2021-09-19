using PictureManager.Domain.Models;
using PictureManager.Utils;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class SegmentsDrawerControl : UserControl {
    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private DragDropFactory _dd;

    public SegmentsDrawerControl() {
      InitializeComponent();

      // Drag to remove from SegmentsGrid
      _dd = new DragDropFactory(SegmentsGrid, SegmentsGrid,
        (src) => src?.DataContext is Segment,
        (src) => (src.DataContext, DragDropEffects.Move),
        (e, data) => data != ((FrameworkElement)e.OriginalSource).DataContext,
        (e, data) => {
          if (App.Core.Segments.SegmentsDrawerToggle(data as Segment))
            _ = ReloadSegments();
        });
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

    private void Segment_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var segment = (Segment)((FrameworkElement)sender).DataContext;
      App.Core.Segments.Select(App.Core.Segments.SegmentsDrawer, segment, isCtrlOn, isShiftOn);
    }
  }
}
