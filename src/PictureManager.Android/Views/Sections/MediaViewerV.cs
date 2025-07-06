using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidX.ViewPager2.Widget;
using PictureManager.Common.Features.MediaItem;
using System;
using System.ComponentModel;
using BitmapFactory = Android.Graphics.BitmapFactory;

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
        _viewPager.Adapter = new MediaViewerAdapter(_dataContext);
        _viewPager.Adapter?.NotifyDataSetChanged();
        break;
      case nameof(MediaViewerVM.IsSwipeEnabled):
        _viewPager.UserInputEnabled = _dataContext.IsSwipeEnabled;
        break;
      case nameof(MediaViewerVM.Current):
        if (_dataContext.IsVisible)
          _viewPager.SetCurrentItem(_dataContext.IndexOfCurrent, false);
        break;
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
  private readonly ImageView _image;

  public MediaViewerMediaItemViewHolder(LinearLayout itemView, MediaViewerVM mediaViewer) : base(itemView) {
    _mediaViewer = mediaViewer;
    _image = new(itemView.Context) {
      LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
    };

    itemView.AddView(_image);
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
      _image.SetImageBitmap(null);
      return;
    }

    _image.SetImageBitmap(BitmapFactory.DecodeFile(mi.FilePath));
  }
}