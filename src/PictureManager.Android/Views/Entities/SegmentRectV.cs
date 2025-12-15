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
  private readonly OnTouchAction _onTouchAction;
  
  private Drawable? _bgSelected;
  private Drawable? _bgWithPerson;
  private Drawable? _bgWithoutPerson;

  public delegate void OnTouchAction(MotionEvent? e, int width, int height, SegmentRectM segmentRect);

  public SegmentRectV(Context context, SegmentRectM dataContext, OnTouchAction onTouchAction) : base(context) {
    _dataContext = dataContext;
    _onTouchAction = onTouchAction;
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
    _onTouchAction(e, Width, Height, _dataContext);
    return false;
  }
}