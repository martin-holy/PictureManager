using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Disposables;
using PictureManager.Common.Features.Segment;
using System;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemFullVM : ObservableObject, IDisposable {
  private readonly BindingScope _bindings = new();
  private readonly SegmentRectVM _segmentRectVM;
  private MediaItemM? _current;

  public ZoomAndPan ZoomAndPan { get; } = new();
  public SegmentRectS SegmentRectS { get; }
  public MediaItemM? Current { get => _current; private set { _current = value; OnPropertyChanged(); } }

  public MediaItemFullVM(MediaViewerVM mediaViewer, SegmentRectVM segmentRectVM, SegmentRectS segmentRectS) {
    _segmentRectVM = segmentRectVM;
    SegmentRectS = segmentRectS;
    _bindings.AddRange([
      ZoomAndPan.Bind(nameof(MH.UI.Controls.ZoomAndPan.ScaleX), x => x.ScaleX, x => SegmentRectS.UpdateScale(x)),
      mediaViewer.Bind(nameof(MediaViewerVM.ExpandToFill), x => x.ExpandToFill, x => ZoomAndPan.ExpandToFill = x),
      segmentRectVM.Bind(nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem, x => { if (x) SegmentRectS.SetMediaItem(Current); })
    ]);
  }

  public void SetMediaItem(MediaItemM? mi) {
    ZoomAndPan.StopAnimation();
    Current = mi;
    _updateSize(mi);

    if (_segmentRectVM.ShowOverMediaItem)
      SegmentRectS.SetMediaItem(mi);
  }

  private void _updateSize(MediaItemM? mi) {
    if (mi == null) return;
    var rotated = mi.Orientation is Imaging.Orientation.Rotate90 or Imaging.Orientation.Rotate270;
    var contentWidth = rotated ? mi.Height : mi.Width;
    var contentHeight = rotated ? mi.Width : mi.Height;
    ZoomAndPan.SetContentSize(contentWidth, contentHeight);
  }

  public void Dispose() {
    _bindings.Dispose();
  }
}