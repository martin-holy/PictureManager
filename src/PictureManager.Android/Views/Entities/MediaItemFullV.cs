using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.ZoomAndPanHost;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Segment;
using System;
using System.Threading;

namespace PictureManager.Android.Views.Entities;

public class MediaItemFullV : FrameLayout, IBindable<MediaItemM> {
  private readonly MediaItemFullVM _mediaItemFullVM;
  private readonly ZoomAndPanHost _zoomAndPanHost;
  private readonly SegmentsRectsV _segmentsRectsV;
  private readonly SegmentRectVM _segmentRectVM;
  private readonly ZoomableImageView _image;
  private readonly ZoomableVideoView _video;
  private readonly BindingScope _bindings = new();
  private CancellationTokenSource? _cts;
  private bool _disposed;

  public MediaItemM? DataContext { get; private set; }

  public MediaItemFullV(Context context, MediaViewerVM mediaViewer, SegmentRectVM segmentRectVM, SegmentRectS segmentRectS) : base(context) {
    _segmentRectVM = segmentRectVM;
    _mediaItemFullVM = new(mediaViewer, segmentRectVM, segmentRectS);
    _mediaItemFullVM.ZoomAndPan.ViewportChangedEvent += _onZoomAndPanViewportChanged;
    _zoomAndPanHost = new(context, _mediaItemFullVM.ZoomAndPan);
    _segmentsRectsV = new(context, segmentRectVM, segmentRectS, _bindings);
    _image = new(context, _mediaItemFullVM.ZoomAndPan);
    _video = new(context, _mediaItemFullVM.ZoomAndPan, Core.VM.Video.MediaPlayer, (AndroidMediaPlayer)Core.VM.Video.UiFullVideo);

    Clickable = true;
    Focusable = true;

    SetClipChildren(false);
    SetClipToPadding(false);

    AddView(_zoomAndPanHost, LPU.FrameMatch());
    AddView(_image, LPU.FrameMatch());
    AddView(_video, LPU.FrameMatch());
    AddView(_segmentsRectsV, LPU.FrameMatch());

    Core.R.Segment.ItemDeletedEvent += _onSegmentItemDeleted;

    _bindings.AddRange([
      // TODO don't do full Bind
      _mediaItemFullVM.ZoomAndPan.Bind(nameof(ZoomAndPan.ExpandToFill), x => x.ExpandToFill, _ => Bind(DataContext)),
      _segmentRectVM.Bind(nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem, _ => _updateSetmentsRectsViewport())
    ]);
  }

  public override bool OnInterceptTouchEvent(MotionEvent? e) {
    if (e == null) return false;
    if (!_segmentRectVM.ShowOverMediaItem) return false;

    var x = e.GetX() - _mediaItemFullVM.ZoomAndPan.TransformX;
    var y = e.GetY() - _mediaItemFullVM.ZoomAndPan.TransformY;

    if (e.ActionMasked == MotionEventActions.Down)
      return _mediaItemFullVM.SegmentRectS.GetBy(x, y) != null || _segmentRectVM.IsEditEnabled;

    return false;
  }

  public override bool OnTouchEvent(MotionEvent? e) {
    if (e == null) return false;
    var x = e.GetX() - _mediaItemFullVM.ZoomAndPan.TransformX;
    var y = e.GetY() - _mediaItemFullVM.ZoomAndPan.TransformY;
    return _segmentsRectsV.HandleTouchEvent(e, x, y);
  }

  private void _onZoomAndPanViewportChanged(object? sender, EventArgs e) =>
    _updateSetmentsRectsViewport();

  private void _updateSetmentsRectsViewport() {
    if (!_segmentRectVM.ShowOverMediaItem) return;
    _mediaItemFullVM.SegmentRectS.UpdateScale(_mediaItemFullVM.ZoomAndPan.ScaleX);
    _segmentsRectsV.SetX((float)_mediaItemFullVM.ZoomAndPan.TransformX);
    _segmentsRectsV.SetY((float)_mediaItemFullVM.ZoomAndPan.TransformY);
  }

  private void _onSegmentItemDeleted(object? sender, SegmentM e) {
    _mediaItemFullVM.SegmentRectS.RemoveIfContains(e);
  }

  public void Bind(MediaItemM? mi) {
    DataContext = mi; 
    if (mi == null) return;
    if (!Core.S.MediaItem.Exists(mi)) return;
    _mediaItemFullVM.SetMediaItem(mi);
    _cts = new CancellationTokenSource();

    if (mi is ImageM) {
      _video.Visibility = ViewStates.Gone;
      _image.Visibility = ViewStates.Visible;
      _ = _image.SetPath(mi.FilePath, mi.Orientation, _cts.Token, Context!);
    }
    else if (mi is VideoM) {
      _video.Visibility = ViewStates.Visible;
      _image.Visibility = ViewStates.Gone;
      _ = _video.SetPath(mi.FilePath, mi.Orientation, _cts.Token, Context!);
    }
  }

  public void Unbind() {
    _cts?.Cancel();
    _cts?.Dispose();
    _cts = null;
    _image.UnsetImage();
    _video.UnsetImage();
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      Core.R.Segment.ItemDeletedEvent -= _onSegmentItemDeleted;
      _image.Dispose();
      _video.Dispose();
      _mediaItemFullVM.ZoomAndPan.ViewportChangedEvent -= _onZoomAndPanViewportChanged;
      _mediaItemFullVM.Dispose();
      _bindings.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }
}