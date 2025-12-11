using Android.Content;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Android.ViewModels;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public class SegmentsRectsV : FrameLayout {
  public SegmentsRectsV(Context context, SegmentRectUiVM dataContext) : base(context) {
    SetClipChildren(false);
    SetClipToPadding(false);

    this.BindVisibility(dataContext.SegmentRectVM, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem);

    // TODO optimize it for adding one by one
    this.Bind(dataContext.SegmentRectS.MediaItemSegmentsRects, (t, c, e) => {
      RemoveAllViews();
      if (c == null) return;
      foreach (var item in c)
        AddView(new SegmentRectV(Context!, item));
    });
  }
}
