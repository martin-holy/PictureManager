using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Segment;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PictureManager.Android.Views.Sections;

public class SegmentsRectsV : FrameLayout {
  private readonly SegmentRectVM _segmentRectVM;
  private readonly SegmentRectS _segmentRectS;
  private readonly IDisposable[] _bindings;

  public SegmentsRectsV(Context context, SegmentRectVM segmentRectVM, SegmentRectS segmentRectS) : base(context) {
    _segmentRectVM = segmentRectVM;
    _segmentRectS = segmentRectS;

    SetClipChildren(false);
    SetClipToPadding(false);

    _bindings = [
      this.BindVisibility(_segmentRectVM, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem),
      this.Bind(_segmentRectS.MediaItemSegmentsRects, _updateSegmentRects)
    ];
  }

  public bool HandleTouchEvent(MotionEvent e, double x, double y) {
    if (!_segmentRectVM.ShowOverMediaItem) return false;

    if (e.ActionMasked == MotionEventActions.Down
      && _segmentRectS.GetBy(x, y) is { } rect
      && _segmentRectS.SetCurrent(rect, x, y))
      return true;

    if (!_segmentRectVM.IsEditEnabled) return false;

    switch (e.ActionMasked) {
      case MotionEventActions.Down:
        if (_segmentRectVM.CanCreateNew)
          _segmentRectS.CreateNew(x, y);
        return true;

      case MotionEventActions.Move:
        if (_segmentRectS.Current == null) return false;
        _segmentRectS.Edit(x, y);
        return true;

      case MotionEventActions.Up:
      case MotionEventActions.Cancel:
        _segmentRectS.EndEdit();
        _segmentRectVM.CanCreateNew = false;
        return true;

      default:
        return false;
    }
  }

  private static void _updateSegmentRects(SegmentsRectsV target, ObservableCollection<SegmentRectM>? col, NotifyCollectionChangedEventArgs e) {
    switch (e.Action) {
      case NotifyCollectionChangedAction.Add:
        if (e.NewItems == null) break;

        foreach (var item in e.NewItems)
          target.AddView(new SegmentRectV(target.Context!, (SegmentRectM)item));

        break;

      case NotifyCollectionChangedAction.Remove:
        if (e.OldItems == null) break;

        foreach (var item in e.OldItems) {
          for (int i = target.ChildCount - 1; i >= 0; i--) {
            if (target.GetChildAt(i) is not SegmentRectV child || !ReferenceEquals(item, child.DataContext)) continue;
            target.RemoveViewAt(i);
            child.Dispose();
          }
        }

        break;

      case NotifyCollectionChangedAction.Reset:
        for (int i = target.ChildCount - 1; i >= 0; i--) {
          if (target.GetChildAt(i) is not { } child) continue;
          target.RemoveViewAt(i);
          child.Dispose();
        }

        if (col == null) break;

        foreach (var item in col)
          target.AddView(new SegmentRectV(target.Context!, item));

        break;
    }
  }

  protected override void Dispose(bool disposing) {
    if (disposing)
      foreach (var bind in _bindings) bind.Dispose();

    base.Dispose(disposing);
  }
}