using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Entities;

public class SegmentRectV : View {
  private readonly BindingScope _bindings = new();

  private Drawable? _bgSelected;
  private Drawable? _bgWithPerson;
  private Drawable? _bgWithoutPerson;

  public SegmentRectM DataContext { get; }
  public delegate bool OnTouchAction(SegmentRectM segmentRect, double x, double y);

  public SegmentRectV(Context context, SegmentRectM dataContext) : base(context) {
    DataContext = dataContext;

    _bindings.AddRange([
      dataContext.Bind(nameof(SegmentRectM.Size), x => x.Size,
        x => LayoutParameters = new FrameLayout.LayoutParams((int)x, (int)x)),
      dataContext.Bind(nameof(SegmentRectM.X), x => x.X, x => SetX((float)x)),
      dataContext.Bind(nameof(SegmentRectM.Y), x => x.Y, x => SetY((float)x)),
      dataContext.Segment.Bind(nameof(SegmentM.Person), x => x.Person, _ => _setBackground()),
      dataContext.Segment.Bind(nameof(SegmentM.IsSelected), x => x.IsSelected, _ => _setBackground())
    ]);
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
    if (disposing) _bindings.Dispose();
    base.Dispose(disposing);
  }
}