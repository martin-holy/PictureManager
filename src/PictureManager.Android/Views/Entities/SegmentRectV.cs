using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Entities;

public class SegmentRectV : View {
  private readonly SegmentRectM _dataContext;
  private readonly SegmentRectS _segmentRectS;
  private static int _borderHitSize = 6;
  private static float _moveHitSize = 24;
  private Drawable? _bgSelected;
  private Drawable? _bgWithPerson;
  private Drawable? _bgWithoutPerson;

  public SegmentRectV(Context context, SegmentRectM dataContext, SegmentRectS segmentRectS) : base(context) {
    _dataContext = dataContext;
    _segmentRectS = segmentRectS;
    _borderHitSize = DisplayU.DpToPx(2);
    _moveHitSize = DisplayU.DpToPx(16) / 2f;
    Clickable = true;

    this.Bind(dataContext, nameof(SegmentRectM.Size), x => x.Size,
      (t, p) => t.LayoutParameters = new FrameLayout.LayoutParams((int)p, (int)p));
    this.Bind(dataContext, nameof(SegmentRectM.X), x => x.X, (t, p) => t.SetX((float)p));
    this.Bind(dataContext, nameof(SegmentRectM.Y), x => x.Y, (t, p) => t.SetY((float)p));
    this.Bind(dataContext.Segment, nameof(SegmentM.Person), x => x.Person, (t, _) => t._setBackground());
    this.Bind(dataContext.Segment, nameof(SegmentM.IsSelected), x => x.IsSelected, (t, _) => t._setBackground());
  }

  private void _setBackground() {
    Background = _dataContext.Segment.IsSelected
      ? _bgSelected ??= _createBackground(Resource.Color.segmentRectSelected)
      : _dataContext.Segment.Person == null
        ? _bgWithoutPerson ??= _createBackground(Resource.Color.segmentRectWithoutPerson)
        : _bgWithPerson ??= _createBackground(Resource.Color.segmentRectWithPerson);
  }

  private static Drawable _createBackground(int strokeColorResId) =>
    BackgroundFactory.Create(
      Resource.Color.c_transparent,
      strokeColorResId,
      Resource.Dimension.border_stroke_width,
      -1,
      Resource.Color.c_black5);

  public override bool OnTouchEvent(MotionEvent? e) {
    if (e?.ActionMasked != MotionEventActions.Down) return false;

    var x = e.GetX();
    var y = e.GetY();
    var w = Width;
    var h = Height;

    if (_isBorderHit(x, y, w, h) || _isMoveHit(x, y, w, h))
      _segmentRectS.SetCurrent(_dataContext, e.RawX, e.RawY);

    return false;
  }

  private static bool _isBorderHit(float x, float y, float w, float h) =>
    x <= _borderHitSize || x >= w - _borderHitSize ||
    y <= _borderHitSize || y >= h - _borderHitSize;

  private static bool _isMoveHit(float x, float y, float w, float h) {
    var cx = w / 2f;
    var cy = h / 2f;

    return
      x >= cx - _moveHitSize && x <= cx + _moveHitSize &&
      y >= cy - _moveHitSize && y <= cy + _moveHitSize;
  }
}