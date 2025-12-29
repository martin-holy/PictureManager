using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentRectVM : ObservableObject {
  private bool _showOverMediaItem;
  private bool _isEditEnabled;
  private bool _canCreateNew;

  public bool ShowOverMediaItem { get => _showOverMediaItem; set { _showOverMediaItem = value; OnPropertyChanged(); } }
  public bool IsEditEnabled { get => _isEditEnabled; set { _isEditEnabled = value; OnPropertyChanged(); } }
  public bool CanCreateNew { get => _canCreateNew; set { _setIfVary(ref _canCreateNew, value); } }
  public List<Tuple<int, int, int, bool>>? SegmentToolTipRects { get; private set; }

  public static RelayCommand<SegmentM> ReloadSegmentToolTipCommand { get; set; } = null!;

  public SegmentRectVM() {
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