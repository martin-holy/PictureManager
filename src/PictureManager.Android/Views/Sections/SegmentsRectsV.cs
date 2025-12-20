using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public class SegmentsRectsV : FrameLayout {
  private readonly SegmentRectVM _segmentRectVM;
  private readonly SegmentRectS _segmentRectS;

  public SegmentsRectsV(Context context, SegmentRectVM segmentRectVM, SegmentRectS segmentRectS) : base(context) {
    _segmentRectVM = segmentRectVM;
    _segmentRectS = segmentRectS;

    SetClipChildren(false);
    SetClipToPadding(false);

    this.BindVisibility(_segmentRectVM, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem);

    // TODO optimize it for adding one by one
    this.Bind(_segmentRectS.MediaItemSegmentsRects, (t, c, e) => {
      t.RemoveAllViews();
      if (c == null) return;
      foreach (var item in c)
        t.AddView(new SegmentRectV(t.Context!, item));
    });
  }

  public bool HandleTouchEvent(MotionEvent e, double x, double y) {
    if (!_segmentRectVM.ShowOverMediaItem) return false;

    if (e.ActionMasked == MotionEventActions.Down) {
      if (_segmentRectS.GetBy(x, y) is not { } rect) return false;
      _segmentRectS.SetCurrent(rect, x, y);
      return true;
    }

    if (!_segmentRectVM.IsEditEnabled) return false;

    switch (e.ActionMasked) {
      case MotionEventActions.Down:
        _segmentRectS.CreateNew(x, y);
        return true;

      case MotionEventActions.Move:
        if (_segmentRectS.Current == null) return false;
        _segmentRectS.Edit(x, y);
        return true;

      case MotionEventActions.Up:
      case MotionEventActions.Cancel:
        _segmentRectS.EndEdit();
        return true;

      default:
        return false;
    }
  }
}
