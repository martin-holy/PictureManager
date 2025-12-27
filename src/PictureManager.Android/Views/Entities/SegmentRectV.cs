using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Segment;
using System;

namespace PictureManager.Android.Views.Entities;

public class SegmentRectV : View {
  private readonly IDisposable[] _bindings;

  private Drawable? _bgSelected;
  private Drawable? _bgWithPerson;
  private Drawable? _bgWithoutPerson;

  public SegmentRectM DataContext { get; }
  public delegate bool OnTouchAction(SegmentRectM segmentRect, double x, double y);

  public SegmentRectV(Context context, SegmentRectM dataContext) : base(context) {
    DataContext = dataContext;

    _bindings = [
      this.Bind(dataContext, nameof(SegmentRectM.Size), x => x.Size,
        (t, p) => t.LayoutParameters = new FrameLayout.LayoutParams((int)p, (int)p)),
      this.Bind(dataContext, nameof(SegmentRectM.X), x => x.X, (t, p) => t.SetX((float)p)),
      this.Bind(dataContext, nameof(SegmentRectM.Y), x => x.Y, (t, p) => t.SetY((float)p)),
      this.Bind(dataContext.Segment, nameof(SegmentM.Person), x => x.Person, (t, _) => t._setBackground()),
      this.Bind(dataContext.Segment, nameof(SegmentM.IsSelected), x => x.IsSelected, (t, _) => t._setBackground())
    ];
  }

  private void _setBackground() {
    Background = DataContext.Segment.IsSelected
      ? _bgSelected ??= _createBackground(Resource.Color.segmentRectSelected)
      : DataContext.Segment.Person == null
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

  protected override void Dispose(bool disposing) {
    if (disposing)
      foreach (var bind in _bindings) bind.Dispose();

    base.Dispose(disposing);
  }
}