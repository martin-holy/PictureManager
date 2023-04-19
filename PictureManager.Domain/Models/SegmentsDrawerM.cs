using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using static MH.Utils.DragDropHelper;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsDrawerM : ObservableObject {
    public SegmentsM SegmentsM { get; }
    public readonly HeaderedListItem<object, string> ToolsTabsItem;
    public CanDragFunc CanDragFunc { get; }
    public CanDropFunc CanDropFunc { get; }
    public DoDropAction DoDropAction { get; }
    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }

    public SegmentsDrawerM(SegmentsM segmentsM) {
      SegmentsM = segmentsM;
      ToolsTabsItem = new(this, "Segments");

      CanDragFunc = CanDrag;
      CanDropFunc = CanDrop;
      DoDropAction = DoDrop;

      SelectCommand = new(Select);
    }

    private object CanDrag(object source) =>
      source is SegmentM segmentM
        ? SegmentsM.GetOneOrSelected(segmentM)
        : null;

    private MH.Utils.DragDropEffects CanDrop(object target, object data, bool haveSameOrigin) {
      if (!haveSameOrigin && !SegmentsM.SegmentsDrawer.Contains(data))
        return MH.Utils.DragDropEffects.Copy;
      if (haveSameOrigin && (data as SegmentM[])?.Contains(target as SegmentM) == false)
        return MH.Utils.DragDropEffects.Move;
      return MH.Utils.DragDropEffects.None;
    }

    private void DoDrop(object data, bool haveSameOrigin) =>
      SegmentsM.SegmentsDrawerUpdate(data as SegmentM[] ?? new[] { data as SegmentM }, !haveSameOrigin);

    private void Select(MouseButtonEventArgs e) {
      if (e.IsSourceDesired && e.DataContext is SegmentM segmentM)
        SegmentsM.Select(SegmentsM.SegmentsDrawer.Cast<SegmentM>().ToList(), segmentM, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
