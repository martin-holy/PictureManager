using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidX.ViewPager2.Widget;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using System;
using System.ComponentModel;

namespace PictureManager.Android.Views.Sections;

public class MediaViewerV : LinearLayout {
  private ViewPager2 _viewPager = null!;
  private MediaViewerVM? _dataContext;

  public MediaViewerVM? DataContext {
    get => _dataContext;
    private set {
      if (_dataContext != null)
        _dataContext.PropertyChanged -= _onDataContextPropertyChanged;
      _dataContext = value;
      if (_dataContext != null)
        _dataContext.PropertyChanged += _onDataContextPropertyChanged;
    }
  }

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
    DataContext = dataContext;
    if (dataContext == null) return this;
    _viewPager.Adapter = new MediaViewerAdapter(dataContext);
    return this;
  }

  private void _onDataContextPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (_dataContext == null) return;

    switch (e.PropertyName) {
      case nameof(MediaViewerVM.IsVisible):
        _dataContext.IsSwipeEnabled = _dataContext.IsVisible;
        break;
      case nameof(MediaViewerVM.MediaItems):
        if (_dataContext.MediaItems.Count == 0) return;
        _viewPager.Adapter = new MediaViewerAdapter(_dataContext);
        _viewPager.SetCurrentItem(_dataContext.IndexOfCurrent, false);
        break;
      case nameof(MediaViewerVM.IsSwipeEnabled):
        _viewPager.UserInputEnabled = _dataContext.IsSwipeEnabled;
        break;
    }
  }

  private class PageChangeCallback : ViewPager2.OnPageChangeCallback {
    private readonly MediaViewerV _viewer;

    public PageChangeCallback(MediaViewerV viewer) => _viewer = viewer;

    public override void OnPageSelected(int position) {
      if (_viewer._dataContext == null) return;
      _viewer._dataContext.Current = _viewer._dataContext.MediaItems[position];
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

public class MediaViewerMediaItemViewHolder : RecyclerView.ViewHolder {
  private readonly MediaViewerVM _mediaViewer;
  private readonly ZoomAndPan _zoomAndPan;
  private readonly ZoomAndPanHost _zoomAndPanHost;

  public MediaViewerMediaItemViewHolder(LinearLayout itemView, MediaViewerVM mediaViewer) : base(itemView) {
    _mediaViewer = mediaViewer;
    _zoomAndPan = new() {
      ExpandToFill = Core.Settings.MediaViewer.ExpandToFill,
      ShrinkToFill = Core.Settings.MediaViewer.ShrinkToFill
    };
    _zoomAndPanHost = new ZoomAndPanHost(itemView.Context!) {
      LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    };

    itemView.AddView(_zoomAndPanHost);
    itemView.Clickable = true;
    itemView.Click += _itemView_Click;
  }

  private void _itemView_Click(object? sender, EventArgs e) {
    _mediaViewer.IsSwipeEnabled = !_mediaViewer.IsSwipeEnabled;
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
}