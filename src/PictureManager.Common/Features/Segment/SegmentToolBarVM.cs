using MH.Utils.BaseClasses;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentToolBarVM(SegmentS segmentS) : ObservableObject {
  public SegmentS SegmentS { get; } = segmentS;
}