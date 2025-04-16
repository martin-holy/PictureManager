using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentRectVM : ObservableObject {
  public List<Tuple<int, int, int, bool>>? SegmentToolTipRects { get; private set; }

  public static AsyncRelayCommand<SegmentRectM> DeleteCommand { get; set; } = null!;
  public static RelayCommand<SegmentM> ReloadSegmentToolTipCommand { get; set; } = null!;

  public SegmentRectVM(SegmentRectS s) {
    DeleteCommand = new((x, _) => s.Delete(x!), x => x != null, MH.UI.Res.IconXCross, "Delete");
    ReloadSegmentToolTipCommand = new(_reloadSegmentToolTip);
  }

  private void _reloadSegmentToolTip(SegmentM? segment) {
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