using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.ViewPager2.Widget;
using MH.UI.Android.Controls.Hosts.ZoomAndPanHost;
using MH.UI.Android.Controls.Recycler;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Segment;
using System;
using System.Threading;

namespace PictureManager.Android.Views.Sections;

public class MediaViewerV : FrameLayout {
  private readonly ViewPager2 _viewPager;
  private readonly BindableAdapter<MediaItemM> _adapter;
  private readonly PageChangeCallback _pageChangeCallback;
  private bool _disposed;

  public MediaViewerVM DataContext { get; }

  public MediaViewerV(Context context, MediaViewerVM dataContext, BindingScope bindings) : base(context) {
    DataContext = dataContext;
    _adapter = new(
      () => dataContext.MediaItems,
      ctx => new MediaViewerMediaItemView(ctx, dataContext, Core.VM.Segment.Rect, new(Core.S.Segment) { EditLimit = 20 }),
      () => new(LPU.Match, LPU.Match));

    SetBackgroundResource(Resource.Color.c_static_ba);

    _pageChangeCallback = new PageChangeCallback(dataContext);
    _viewPager = new(context) { Adapter = _adapter };
    _viewPager.RegisterOnPageChangeCallback(_pageChangeCallback);
    AddView(_viewPager, LPU.FrameMatch());

    bindings.AddRange([
      dataContext.Bind(nameof(MediaViewerVM.MediaItems), x => x.MediaItems, x => {
        _adapter.NotifyDataSetChanged();
        if (x!.Count > 0)
          _viewPager.SetCurrentItem(DataContext.IndexOfCurrent, false);
      }),

      dataContext.Bind(nameof(MediaViewerVM.UserInputMode), x => x.UserInputMode, x =>
        _viewPager.UserInputEnabled = x == MediaViewerVM.UserInputModes.Browse)
    ]);
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      _viewPager.UnregisterOnPageChangeCallback(_pageChangeCallback);
      _viewPager.Adapter = null;
      _adapter.Dispose();
      _pageChangeCallback.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }

  private class PageChangeCallback(MediaViewerVM mediaViewerVM) : ViewPager2.OnPageChangeCallback {
    public override void OnPageSelected(int position) {
      mediaViewerVM.GoTo(position);
    }
  }

  private class MediaViewerMediaItemView : FrameLayout, IBindable<MediaItemM> {
    private readonly MediaItemFullVM _mediaItemFullVM;
    private readonly MediaViewerVM _mediaViewer;
    private readonly ZoomAndPanHost _zoomAndPanHost;
    private readonly SegmentsRectsV _segmentsRectsV;
    private readonly SegmentRectVM _segmentRectVM;
    private readonly BindingScope _bindings = new();
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public MediaItemM? DataContext { get; private set; }

    public MediaViewerMediaItemView(Context context, MediaViewerVM mediaViewer, SegmentRectVM segmentRectVM, SegmentRectS segmentRectS) : base(context) {
      _mediaViewer = mediaViewer;
      _segmentRectVM = segmentRectVM;
      _mediaItemFullVM = new(mediaViewer, segmentRectVM, segmentRectS);

      _zoomAndPanHost = new(context, _mediaItemFullVM.ZoomAndPan);
      _zoomAndPanHost.ImageTransformUpdatedEvent += _onImageTransformUpdated;

      _segmentsRectsV = new(context, segmentRectVM, segmentRectS, _bindings);      

      Clickable = true;
      Focusable = true;
      SetClipChildren(false);
      SetClipToPadding(false);
      AddView(_zoomAndPanHost, LPU.FrameMatch());
      AddView(_segmentsRectsV, LPU.FrameMatch());

      Core.R.Segment.ItemDeletedEvent += _onSegmentItemDeleted;

      _bindings.AddRange([
        // TODO don't do full Bind
        _mediaItemFullVM.ZoomAndPan.Bind(nameof(ZoomAndPan.ExpandToFill), x => x.ExpandToFill, _ => Bind(DataContext)),

        _segmentRectVM.Bind(nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem,
          show => { if (show) _onImageTransformUpdated(null, EventArgs.Empty); })
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

    private void _onImageTransformUpdated(object? sender, EventArgs e) {
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

      if (mi is ImageM) {
        _cts = new CancellationTokenSource();
        _ = _zoomAndPanHost.SetImagePathAsync(mi.FilePath, mi.Orientation, _cts.Token, Context!);
      }
      else if (mi is VideoM)
        _zoomAndPanHost.SetVideoPath(mi.FilePath);
    }

    public void Unbind() {
      _cts?.Cancel();
      _cts?.Dispose();
      _cts = null;
      _zoomAndPanHost.UnsetImage();
      _zoomAndPanHost.SetVideoPath(null);
    }

    protected override void Dispose(bool disposing) {
      if (_disposed) return;
      if (disposing) {
        Core.R.Segment.ItemDeletedEvent -= _onSegmentItemDeleted;
        _zoomAndPanHost.ImageTransformUpdatedEvent -= _onImageTransformUpdated;
        _mediaItemFullVM.Dispose();
        _bindings.Dispose();
      }
      _disposed = true;
      base.Dispose(disposing);
    }
  }
}