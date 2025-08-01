﻿using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.MediaItem;
using System.Threading.Tasks;

namespace PictureManager.Android.Views.Entities;

public class MediaItemThumbFullV : LinearLayout {
  private ImageView _image = null!;

  public MediaItemM? DataContext { get; private set; }

  public MediaItemThumbFullV(Context context) : base(context) => _initialize(context);
  public MediaItemThumbFullV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context);
  protected MediaItemThumbFullV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!);

  private void _initialize(Context context) {
    LayoutInflater.From(context)!.Inflate(Resource.Layout.pm_dt_media_item_thumb_full, this, true);
    _image = FindViewById<ImageView>(Resource.Id.image)!;
  }

  public MediaItemThumbFullV Bind(MediaItemM? mi) {
    DataContext = mi;
    if (mi == null) {
      _image.SetImageBitmap(null);
      return this;
    }

    _loadThumbnailAsync(mi.FilePath, _image, Context!);
    return this;
  }

  private static async void _loadThumbnailAsync(string imagePath, ImageView imageView, Context context) {
    var thumbnail = await Task.Run(() => MediaStoreU.GetThumbnailBitmap(imagePath, context));
    MH.Utils.Tasks.Dispatch(() => imageView.SetImageBitmap(thumbnail));
  }
}