using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.MediaItem;
using System.Threading.Tasks;

namespace PictureManager.Android.Views;

public class MediaItemThumbFullV : LinearLayout {
  private ImageView _image = null!;
  private Context _context = null!;
  private MediaItemM? _dataContext;

  public MediaItemM? DataContext { get => _dataContext; set { _dataContext = value; _bind(value); } }

  public MediaItemThumbFullV(Context context) : base(context) => _initialize(context, null);
  public MediaItemThumbFullV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context, attrs);
  protected MediaItemThumbFullV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!, null);

  private void _initialize(Context context, IAttributeSet? attrs) {
    _context = context;
    LayoutInflater.From(context)!.Inflate(Resource.Layout.pm_dt_media_item_thumb_full, this, true);
    _image = FindViewById<ImageView>(Resource.Id.image)!;
  }

  private void _bind(MediaItemM? mi) {
    if (mi == null) {
      _image.SetImageBitmap(null);
      return;
    }

    LayoutParameters = new ViewGroup.LayoutParams(mi.ThumbWidth, mi.ThumbHeight);
    _loadThumbnailAsync(mi.FilePath, _image, _context);
  }

  private async void _loadThumbnailAsync(string imagePath, ImageView imageView, Context context) {
    var thumbnail = await Task.Run(() => MediaStoreU.GetThumbnailBitmap(imagePath, context));
    MH.Utils.Tasks.Dispatch(() => imageView.SetImageBitmap(thumbnail));
  }
}