using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidX.ViewPager2.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using System;

namespace PictureManager.Android.Views.Sections;

public class MediaViewerV : LinearLayout {
  private readonly ViewPager2 _viewPager;
  private readonly MediaViewerAdapter _adapter;
  private readonly PageChangeCallback _pageChangeCallback;
  private bool _disposed;

  public MediaViewerVM DataContext { get; }

  public MediaViewerV(Context context, MediaViewerVM dataContext) : base(context) {
    DataContext = dataContext;
    _adapter = new MediaViewerAdapter(dataContext);

    SetBackgroundResource(Resource.Color.c_static_ba);

    _pageChangeCallback = new PageChangeCallback(this);
    _viewPager = new(context) { Adapter = _adapter };
    _viewPager.RegisterOnPageChangeCallback(_pageChangeCallback);
    AddView(_viewPager, new LayoutParams(LPU.Match, LPU.Match));

    this.Bind(dataContext, x => x.IsVisible, (v, p) =>
      v.DataContext.UserInputMode = p
      ? MediaViewerVM.UserInputModes.Browse
      : MediaViewerVM.UserInputModes.Disabled);

    this.Bind(dataContext, x => x.MediaItems, (v, p) => {
      v._adapter.NotifyDataSetChanged();
      if (p.Count > 0)
        v._viewPager.SetCurrentItem(v.DataContext.IndexOfCurrent, false);
    });

    this.Bind(dataContext, x => x.UserInputMode, (v, p) =>
      v._viewPager.UserInputEnabled = p == MediaViewerVM.UserInputModes.Browse);
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
      new MediaViewerMediaItemViewHolder(parent.Context!, _mediaViewer);

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) =>
      ((MediaViewerMediaItemViewHolder)holder).Bind(_mediaViewer.MediaItems[position]);
  }

  private class MediaViewerMediaItemViewHolder : RecyclerView.ViewHolder {
    private readonly MediaViewerVM _mediaViewer;
    private readonly ZoomAndPan _zoomAndPan;
    private readonly ZoomAndPanHost _zoomAndPanHost;
    private bool _disposed;

    public MediaViewerMediaItemViewHolder(Context context, MediaViewerVM mediaViewer) : base(_createContainerView(context)) {
      _mediaViewer = mediaViewer;
      _zoomAndPan = new() {
        ExpandToFill = Core.Settings.MediaViewer.ExpandToFill,
        ShrinkToFill = Core.Settings.MediaViewer.ShrinkToFill
      };

      this.Bind(_zoomAndPan, x => x.IsZoomed, (v, p) => {
        v._mediaViewer.UserInputMode = p
          ? MediaViewerVM.UserInputModes.Transform
          : MediaViewerVM.UserInputModes.Browse;
      });

      _zoomAndPanHost = new(context, _zoomAndPan);
      _zoomAndPanHost.SingleTapConfirmedEvent += _onSingleTap;

      var container = (LinearLayout)ItemView;
      container.AddView(_zoomAndPanHost, new LinearLayout.LayoutParams(LPU.Match, LPU.Match));
    }

    private static LinearLayout _createContainerView(Context context) {
      return new(context) {
        LayoutParameters = new RecyclerView.LayoutParams(LPU.Match, LPU.Match),
      };
    }

    private void _onSingleTap(object? sender, EventArgs e) {
      _mediaViewer.UserInputMode = _mediaViewer.UserInputMode != MediaViewerVM.UserInputModes.Disabled
        ? MediaViewerVM.UserInputModes.Disabled
        : _zoomAndPan.IsZoomed
          ? MediaViewerVM.UserInputModes.Transform
          : MediaViewerVM.UserInputModes.Browse;
    }

    public void Bind(MediaItemM? mi) {
      if (mi == null) {
        _zoomAndPanHost.SetImagePath(null);
        _zoomAndPanHost.SetVideoPath(null);
        return;
      }

      var rotated = mi.Orientation is Imaging.Orientation.Rotate90 or Imaging.Orientation.Rotate270;
      var width = rotated ? mi.Height : mi.Width;
      var height = rotated ? mi.Width : mi.Height;
      _zoomAndPan.ContentWidth = width;
      _zoomAndPan.ContentHeight = height;
      _zoomAndPan.ScaleToFitContent(width, height);

      if (mi is ImageM)
        _zoomAndPanHost.SetImagePath(mi.FilePath, mi.Orientation);
      else if (mi is VideoM)
        _zoomAndPanHost.SetVideoPath(mi.FilePath);

      _zoomAndPanHost.UpdateImageTransform();
    }

    protected override void Dispose(bool disposing) {
      if (_disposed) return;
      if (disposing) {
        _zoomAndPanHost.SingleTapConfirmedEvent -= _onSingleTap;
      }
      _disposed = true;
      base.Dispose(disposing);
    }
  }
}