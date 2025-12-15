using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Android.ViewModels;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public class SegmentsRectsV : FrameLayout {
  private readonly SegmentRectUiVM _dataContext;
  private readonly int _borderHitSize = DisplayU.DpToPx(2);
  private readonly float _moveHitSize = DisplayU.DpToPx(16) / 2f;

  public SegmentsRectsV(Context context, SegmentRectUiVM dataContext) : base(context) {
    _dataContext = dataContext;

    SetClipChildren(false);
    SetClipToPadding(false);

    this.BindVisibility(_dataContext.SegmentRectVM, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem);

    // TODO optimize it for adding one by one
    this.Bind(_dataContext.SegmentRectS.MediaItemSegmentsRects, (t, c, e) => {
      t.RemoveAllViews();
      if (c == null) return;
      foreach (var item in c)
        t.AddView(new SegmentRectV(t.Context!, item, t._onSegmentRectTouch));
    });
  }

  private void _onSegmentRectTouch(MotionEvent? e, int width, int height, SegmentRectM segmentRect) {
    if (e?.ActionMasked != MotionEventActions.Down) return;

    var x = e.GetX();
    var y = e.GetY();

    if (_isBorderHit(x, y, width, height) || _isMoveHit(x, y, width, height))
      _dataContext.SegmentRectS.SetCurrent(segmentRect, e.RawX, e.RawY);
  }

  private bool _isBorderHit(float x, float y, float w, float h) =>
    x <= _borderHitSize || x >= w - _borderHitSize ||
    y <= _borderHitSize || y >= h - _borderHitSize;

  private bool _isMoveHit(float x, float y, float w, float h) {
    var cx = w / 2f;
    var cy = h / 2f;

    return
      x >= cx - _moveHitSize && x <= cx + _moveHitSize &&
      y >= cy - _moveHitSize && y <= cy + _moveHitSize;
  }
}
