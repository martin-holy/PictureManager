using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentRectVM : ObservableObject {
  public List<Tuple<int, int, int, bool>>? SegmentToolTipRects { get; private set; }

  public static RelayCommand<SegmentM> SegmentToolTipReloadCommand { get; set; } = null!;

  public SegmentRectVM() {
    SegmentToolTipReloadCommand = new(SegmentToolTipReload);
  }

  private void SegmentToolTipReload(SegmentM? segment) {
    if (segment?.MediaItem.Segments == null)
      SegmentToolTipRects = null;
    else {
      segment.MediaItem.SetThumbSize();
      segment.MediaItem.SetInfoBox();
      SegmentToolTipRects = SegmentRectS.GetMediaItemSegmentsRects(segment);
    }

    OnPropertyChanged(nameof(SegmentToolTipRects));
  }
}