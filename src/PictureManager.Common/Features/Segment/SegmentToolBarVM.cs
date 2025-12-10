using MH.Utils.BaseClasses;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentToolBarVM : ObservableObject {
  public SegmentVM SegmentVM { get; }

  public SegmentToolBarVM(SegmentVM segmentVM) {
    SegmentVM = segmentVM;
  }
}