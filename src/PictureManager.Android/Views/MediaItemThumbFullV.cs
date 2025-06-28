using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.UI.Interfaces;
using PictureManager.Common.Features.MediaItem;
using System.Threading.Tasks;

namespace PictureManager.Android.Views;

public class MediaItemThumbFullV : LinearLayout {
  private ImageView _image = null!;

  public MediaItemM? DataContext { get; private set; }

  public MediaItemThumbFullV(Context context) : base(context) => _initialize(context, null);
  public MediaItemThumbFullV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context, attrs);
  protected MediaItemThumbFullV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!, null);

  private void _initialize(Context context, IAttributeSet? attrs) {
    LayoutInflater.From(context)!.Inflate(Resource.Layout.pm_dt_media_item_thumb_full, this, true);
    _image = FindViewById<ImageView>(Resource.Id.image)!;
  }

  public MediaItemThumbFullV Bind(MediaItemM? mi, MediaItemCollectionView view, ICollectionViewGroup group) {
    DataContext = mi;
    if (mi == null) {
      _image.SetImageBitmap(null);
      return this;
    }

    LayoutParameters = new ViewGroup.LayoutParams(
      DisplayU.GetDP(view.GetItemSize(group.ViewMode, mi, true)),
      DisplayU.GetDP(view.GetItemSize(group.ViewMode, mi, false)));
    _loadThumbnailAsync(mi.FilePath, _image, Context!);
    return this;
  }

  private async void _loadThumbnailAsync(string imagePath, ImageView imageView, Context context) {
    var thumbnail = await Task.Run(() => MediaStoreU.GetThumbnailBitmap(imagePath, context));
    MH.Utils.Tasks.Dispatch(() => imageView.SetImageBitmap(thumbnail));
  }
}