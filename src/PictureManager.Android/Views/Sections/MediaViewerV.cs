using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidX.ViewPager2.Widget;
using MH.Utils.Extensions;
using PictureManager.Common.Features.MediaItem;
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
    if (e.Is(nameof(MediaViewerVM.MediaItems))) {
      _viewPager.Adapter = new MediaViewerAdapter(DataContext!);
      _viewPager.Adapter?.NotifyDataSetChanged();
    }
  }
}

public class MediaViewerAdapter(MediaViewerVM vm) : RecyclerView.Adapter {
  public override int ItemCount => vm.MediaItems.Count;

  public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) =>
    MediaViewerMediaItemViewHolder.Create(parent);

  public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) =>
    ((MediaViewerMediaItemViewHolder)holder).Bind(vm.MediaItems[position]);
}

public class MediaViewerMediaItemViewHolder : RecyclerView.ViewHolder {
  private readonly ImageView _image;

  public MediaViewerMediaItemViewHolder(LinearLayout itemView) : base(itemView) {
    _image = new(itemView.Context) {
      LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
    };

    itemView.AddView(_image);
  }

  public static MediaViewerMediaItemViewHolder Create(ViewGroup parent) =>
    new(new LinearLayout(parent.Context) {
      LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    });

  public void Bind(MediaItemM? mi) {
    if (mi == null) {
      _image.SetImageBitmap(null);
      return;
    }

    _image.SetImageBitmap(BitmapFactory.DecodeFile(mi.FilePath));
  }
}