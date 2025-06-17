using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.MAUI.Droid.Views;

public class MediaItemThumbFullV : LinearLayout {
  private TextView _name = null!;
  private MediaItemM? _dataContext;

  public MediaItemM? DataContext { get => _dataContext; set { _dataContext = value; _bind(value); } }

  public MediaItemThumbFullV(Context context) : base(context) => _initialize(context, null);
  public MediaItemThumbFullV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context, attrs);
  protected MediaItemThumbFullV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!, null);

  private void _initialize(Context context, IAttributeSet? attrs) {
    LayoutInflater.From(context)!.Inflate(Resource.Layout.pm_dt_media_item_thumb_full, this, true);
    _name = FindViewById<TextView>(Resource.Id.name)!;
  }

  private void _bind(MediaItemM? mi) {
    if (mi == null) return;
    _name.Text = mi.FileName;
  }
}