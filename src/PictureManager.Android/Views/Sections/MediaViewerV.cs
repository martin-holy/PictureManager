using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidX.ViewPager2.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils.Extensions;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using System;
using System.ComponentModel;

namespace PictureManager.Android.Views.Sections;

public class MediaViewerV : LinearLayout, IDisposable {
  private bool _disposed;
  private ViewPager2 _viewPager = null!;
  private MediaViewerAdapter _adapter = null!;
  private MediaViewerVM? _dataContext;

  public MediaViewerVM DataContext { get => _dataContext ?? throw new InvalidOperationException(ErrorMessages.DataContextNotInitialized); }

  public MediaViewerV(Context context) : base(context) => _initialize(context);
  public MediaViewerV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context);
  protected MediaViewerV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!);

  private void _initialize(Context context) {
    SetBackgroundResource(Resource.Color.c_static_ba);
    _viewPager = new(context) {
      LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    };
    _viewPager.RegisterOnPageChangeCallback(new PageChangeCallback(this));
    AddView(_viewPager);
  }

  public MediaViewerV Bind(MediaViewerVM dataContext) {
    _updateEvents(_dataContext, dataContext);
    _dataContext = dataContext;
    if (_dataContext == null) return this;
    _adapter = new MediaViewerAdapter(dataContext);
    _viewPager.Adapter = _adapter;
    return this;
  }

  private void _updateEvents(MediaViewerVM? oldValue, MediaViewerVM? newValue) {
    if (oldValue != null) oldValue.PropertyChanged -= _onDataContextPropertyChanged;
    if (newValue != null) newValue.PropertyChanged += _onDataContextPropertyChanged;
  }

  private void _onDataContextPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    switch (e.PropertyName) {
      case nameof(MediaViewerVM.IsVisible):
        DataContext.UserInputMode = DataContext.IsVisible
          ? MediaViewerVM.UserInputModes.Browse
          : MediaViewerVM.UserInputModes.Disabled;
        break;
      case nameof(MediaViewerVM.MediaItems):
        _adapter.NotifyDataSetChanged();
        if (DataContext.MediaItems.Count > 0)
          _viewPager.SetCurrentItem(DataContext.IndexOfCurrent, false);
        break;
      case nameof(MediaViewerVM.UserInputMode):
        _viewPager.UserInputEnabled = DataContext.UserInputMode == MediaViewerVM.UserInputModes.Browse;
        break;
    }
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;

    if (disposing) {
      _viewPager.Adapter = null;
      _viewPager.Dispose();
      _adapter.Dispose();
    }

    _disposed = true;
    base.Dispose(disposing);
  }

  private class PageChangeCallback : ViewPager2.OnPageChangeCallback {
    private readonly MediaViewerV _viewer;

    public PageChangeCallback(MediaViewerV viewer) => _viewer = viewer;

    public override void OnPageSelected(int position) {
      _viewer.DataContext.Current = _viewer.DataContext.MediaItems[position];
    }
  }
}

public class MediaViewerAdapter(MediaViewerVM mediaViewer) : RecyclerView.Adapter {
  public override int ItemCount => mediaViewer.MediaItems.Count;

  public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) =>
    MediaViewerMediaItemViewHolder.Create(parent, mediaViewer);

  public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) =>
    ((MediaViewerMediaItemViewHolder)holder).Bind(mediaViewer.MediaItems[position]);
}

public class MediaViewerMediaItemViewHolder : RecyclerView.ViewHolder, IDisposable {
  private bool _disposed;
  private readonly WeakReference<MediaViewerVM> _mediaViewerWeakRef;
  private readonly ZoomAndPan _zoomAndPan;
  private readonly ZoomAndPanHost _zoomAndPanHost;

  public MediaViewerMediaItemViewHolder(LinearLayout itemView, MediaViewerVM mediaViewer) : base(itemView) {
    _mediaViewerWeakRef = new WeakReference<MediaViewerVM>(mediaViewer);
    _zoomAndPan = new() {
      ExpandToFill = Core.Settings.MediaViewer.ExpandToFill,
      ShrinkToFill = Core.Settings.MediaViewer.ShrinkToFill
    };
    _zoomAndPan.PropertyChanged += _onZoomAndPanPropertyChanged;

    _zoomAndPanHost = new ZoomAndPanHost(itemView.Context!) {
      LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    };
    _zoomAndPanHost.SingleTapConfirmedEvent += _onSingleTap;

    itemView.AddView(_zoomAndPanHost);
  }

  private void _onZoomAndPanPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(ZoomAndPan.IsZoomed)) && _mediaViewerWeakRef.TryGetTarget(out var mediaViewer))
      mediaViewer.UserInputMode = (sender as ZoomAndPan)!.IsZoomed
        ? MediaViewerVM.UserInputModes.Transform
        : MediaViewerVM.UserInputModes.Browse;
  }

  private void _onSingleTap(object? sender, EventArgs e) {
    if (!_mediaViewerWeakRef.TryGetTarget(out var mediaViewer)) return;
    mediaViewer.UserInputMode = mediaViewer.UserInputMode != MediaViewerVM.UserInputModes.Disabled
      ? MediaViewerVM.UserInputModes.Disabled
      : _zoomAndPan.IsZoomed
        ? MediaViewerVM.UserInputModes.Transform
        : MediaViewerVM.UserInputModes.Browse;
  }

  public static MediaViewerMediaItemViewHolder Create(ViewGroup parent, MediaViewerVM mediaViewer) =>
    new(new LinearLayout(parent.Context) {
      LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    }, mediaViewer);

  public void Bind(MediaItemM? mi) {
    if (mi == null) {
      _zoomAndPanHost.SetImageBitmap(null);
      return;
    }

    var rotated = mi.Orientation is MH.Utils.Orientation.Rotate90 or MH.Utils.Orientation.Rotate270;
    var width = rotated ? mi.Height : mi.Width;
    var height = rotated ? mi.Width : mi.Height;
    _zoomAndPan.ContentWidth = width;
    _zoomAndPan.ContentHeight = height;
    _zoomAndPanHost.Bind(_zoomAndPan);
    _zoomAndPan.ScaleToFitContent(width, height);
    _zoomAndPanHost.SetImageBitmap(global::Android.Graphics.BitmapFactory.DecodeFile(mi.FilePath));
    _zoomAndPanHost.UpdateImageTransform();
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;

    if (disposing) {
      _zoomAndPanHost.Dispose();
      _zoomAndPan.PropertyChanged -= _onZoomAndPanPropertyChanged;
    }

    _disposed = true;
    base.Dispose(disposing);
  }
}