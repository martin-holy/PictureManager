using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Interfaces;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views;

public class MediaItemsViewV : LinearLayout {
  private CollectionViewHost _host = null!;

  public MediaItemsViewVM? DataContext { get; private set; }

  public MediaItemsViewV(Context context) : base(context) => _initialize(context);
  public MediaItemsViewV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context);
  protected MediaItemsViewV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!);

  private void _initialize(Context context) {
    _host = new(context) { GetItemView = _getItemView };
    AddView(_host);
  }

  private View? _getItemView(LinearLayout container, ICollectionViewGroup group, object? item) {
    if (item is not MediaItemM mi) return null;

    return group.GetItemTemplateName() switch {
      "PM.DT.MediaItem.Thumb-Full" => new MediaItemThumbFullV(container.Context!).Bind(mi, DataContext!, group),
      _ => null,
    };
  }

  public MediaItemsViewV Bind(MediaItemsViewVM? dataContext) {
    DataContext = dataContext;
    if (DataContext == null) return this;
    _host.ViewModel = DataContext;
    return this;
  }
}