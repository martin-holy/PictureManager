using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Disposables;
using PictureManager.Common.Features.Segment;
using System;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemFullVM : ObservableObject, IDisposable {
  private readonly BindingScope _bindings = new();
  private MediaItemM? _current;

  public ZoomAndPan ZoomAndPan { get; } = new();
  public SegmentRectS SegmentRectS { get; } = new(Core.S.Segment);
  public MediaItemM? Current { get => _current; private set { _current = value; OnPropertyChanged(); } }
  public Action OnExpandToFillChanged { get; set; } = delegate { }; // TODO update size for WPF
  public Action OnShrinkToFillChanged { get; set; } = delegate { }; // TODO update size for WPF

  public MediaItemFullVM(MediaViewerVM mediaViewer) {
    _bindings.AddRange([
      ZoomAndPan.Bind(nameof(MH.UI.Controls.ZoomAndPan.ScaleX), x => x.ScaleX, x => SegmentRectS.UpdateScale(x)),
      mediaViewer.Bind(nameof(MediaViewerVM.ExpandToFill), x => x.ExpandToFill, x => { ZoomAndPan.ExpandToFill = x; OnExpandToFillChanged(); }),
      mediaViewer.Bind(nameof(MediaViewerVM.ShrinkToFill), x => x.ShrinkToFill, x => { ZoomAndPan.ShrinkToFill = x; OnShrinkToFillChanged(); })
    ]);
  }

  public void UpdateSize(MediaItemM mi) {
    var rotated = mi.Orientation is Imaging.Orientation.Rotate90 or Imaging.Orientation.Rotate270;
    var contentWidth = rotated ? mi.Height : mi.Width;
    var contentHeight = rotated ? mi.Width : mi.Height;
    ZoomAndPan.ScaleToFitContent(contentWidth, contentHeight);
  }

  public void SetMediaItem(MediaItemM? mi) {
    Current = mi;
  }

  public void Dispose() {
    _bindings.Dispose();
  }
}