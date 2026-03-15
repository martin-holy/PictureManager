using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
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
  private readonly MediaViewerAdapter _adapter;
  private readonly PageChangeCallback _pageChangeCallback;
  private bool _disposed;

  public MediaViewerVM DataContext { get; }

  public MediaViewerV(Context context, MediaViewerVM dataContext, BindingScope bindings) : base(context) {
    DataContext = dataContext;
    _adapter = new MediaViewerAdapter(dataContext);

    SetBackgroundResource(Resource.Color.c_static_ba);

    _pageChangeCallback = new PageChangeCallback(this);
    _viewPager = new(context) { Adapter = _adapter };
    _viewPager.RegisterOnPageChangeCallback(_pageChangeCallback);
    AddView(_viewPager, LPU.FrameMatch());

    bindings.AddRange([
      dataContext.Bind(nameof(MediaViewerVM.IsVisible), x => x.IsVisible, x =>
        DataContext.UserInputMode = x
          ? MediaViewerVM.UserInputModes.Browse
          : MediaViewerVM.UserInputModes.Disabled),

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

  private class PageChangeCallback(MediaViewerV _viewer) : ViewPager2.OnPageChangeCallback {
    public override void OnPageSelected(int position) {
      _viewer.DataContext.Current = _viewer.DataContext.MediaItems[position];
    }
  }

  private class MediaViewerAdapter(MediaViewerVM _mediaViewer) : RecyclerView.Adapter {
    public override int ItemCount => _mediaViewer.MediaItems.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) =>
      new BaseViewHolder(new MediaViewerMediaItemView(parent.Context!, _mediaViewer), new(LPU.Match, LPU.Match));

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) {
      (holder.ItemView as IBindable<MediaItemM>)?.Rebind(_mediaViewer.MediaItems[position]); ;
    }

    public override void OnViewRecycled(Java.Lang.Object holder) {
      (((RecyclerView.ViewHolder)holder).ItemView as IUnbindable)?.Unbind();
      base.OnViewRecycled(holder);
    }
  }

  private class MediaViewerMediaItemView : FrameLayout, IBindable<MediaItemM> {
    private readonly MediaViewerVM _mediaViewer;
    private readonly ZoomAndPan _zoomAndPan;
    private readonly ZoomAndPanHost _zoomAndPanHost;
    private readonly SegmentsRectsV _segmentsRectsV;
    private readonly SegmentRectS _segmentRectS;
    private readonly SegmentRectVM _segmentRectVM;
    private readonly BindingScope _bindings = new();
    private MediaItemM? _dataContext;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public MediaViewerMediaItemView(Context context, MediaViewerVM mediaViewer) : base(context) {
      _mediaViewer = mediaViewer;
      _zoomAndPan = new();

      _zoomAndPanHost = new(context, _zoomAndPan);
      _zoomAndPanHost.ImageTransformUpdatedEvent += _onImageTransformUpdated;

      _segmentRectS = new(Core.S.Segment) { EditLimit = 20 };
      _segmentRectVM = Core.VM.Segment.Rect;
      _segmentsRectsV = new SegmentsRectsV(context, _segmentRectVM, _segmentRectS, _bindings);      

      Clickable = true;
      Focusable = true;
      SetClipChildren(false);
      SetClipToPadding(false);
      AddView(_zoomAndPanHost, LPU.FrameMatch());
      AddView(_segmentsRectsV, LPU.FrameMatch());

      Core.R.Segment.ItemDeletedEvent += _onSegmentItemDeleted;

      _bindings.AddRange([
        _zoomAndPan.Bind(nameof(ZoomAndPan.IsZoomed), x => x.IsZoomed, x => {
          _mediaViewer.UserInputMode = x
            ? MediaViewerVM.UserInputModes.Transform
            : MediaViewerVM.UserInputModes.Browse;
        }),

        _zoomAndPan.Bind(nameof(ZoomAndPan.ScaleX), x => x.ScaleX, x => _segmentRectS.UpdateScale(x)),

        _segmentRectVM.Bind(nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem,
          show => {
            if (!show) return;
            _onImageTransformUpdated(null, EventArgs.Empty);
            _segmentRectS.ReloadMediaItemSegmentRects();
          }),

        mediaViewer.Bind(nameof(MediaViewerVM.ExpandToFill), x => x.ExpandToFill,
          x => { _zoomAndPan.ExpandToFill = x; Bind(_dataContext); }),

        mediaViewer.Bind(nameof(MediaViewerVM.ShrinkToFill), x => x.ShrinkToFill,
          x => { _zoomAndPan.ShrinkToFill = x; Bind(_dataContext); })
      ]);
    }

    public override bool OnInterceptTouchEvent(MotionEvent? e) {
      if (e == null) return false;
      if (!_segmentRectVM.ShowOverMediaItem) return false;

      var x = e.GetX() - _zoomAndPan.TransformX;
      var y = e.GetY() - _zoomAndPan.TransformY;

      if (e.ActionMasked == MotionEventActions.Down)
        return _segmentRectS.GetBy(x, y) != null || _segmentRectVM.IsEditEnabled;

      return false;
    }

    public override bool OnTouchEvent(MotionEvent? e) {
      if (e == null) return false;
      var x = e.GetX() - _zoomAndPan.TransformX;
      var y = e.GetY() - _zoomAndPan.TransformY;
      return _segmentsRectsV.HandleTouchEvent(e, x, y);
    }

    private void _onImageTransformUpdated(object? sender, EventArgs e) {
      if (!_segmentRectVM.ShowOverMediaItem) return;
      _segmentRectS.UpdateScale(_zoomAndPan.ScaleX);
      _segmentsRectsV.SetX((float)_zoomAndPan.TransformX);
      _segmentsRectsV.SetY((float)_zoomAndPan.TransformY);
    }

    private void _onSegmentItemDeleted(object? sender, SegmentM e) {
      _segmentRectS.RemoveIfContains(e);
    }

    public void Bind(MediaItemM? mi) {
      _dataContext = mi;
      if (mi == null) return;

      var rotated = mi.Orientation is Imaging.Orientation.Rotate90 or Imaging.Orientation.Rotate270;
      var width = rotated ? mi.Height : mi.Width;
      var height = rotated ? mi.Width : mi.Height;
      _zoomAndPan.ContentWidth = width;
      _zoomAndPan.ContentHeight = height;

      if (mi is ImageM) {
        _cts = new CancellationTokenSource();
        _ = _zoomAndPanHost.SetImagePathAsync(mi.FilePath, width, height, mi.Orientation, _cts.Token, Context!);
      }
      else if (mi is VideoM)
        _zoomAndPanHost.SetVideoPath(mi.FilePath);

      _zoomAndPanHost.Post(() => _segmentRectS.SetMediaItem(mi, _segmentRectVM.ShowOverMediaItem));
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
        _bindings.Dispose();
      }
      _disposed = true;
      base.Dispose(disposing);
    }
  }
}