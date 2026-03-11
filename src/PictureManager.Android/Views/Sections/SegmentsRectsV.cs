using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Segment;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace PictureManager.Android.Views.Sections;

public class SegmentsRectsV : FrameLayout {
  private readonly SegmentRectVM _segmentRectVM;
  private readonly SegmentRectS _segmentRectS;

  public SegmentsRectsV(Context context, SegmentRectVM segmentRectVM, SegmentRectS segmentRectS, BindingScope bindings) : base(context) {
    _segmentRectVM = segmentRectVM;
    _segmentRectS = segmentRectS;

    SetClipChildren(false);
    SetClipToPadding(false);

    this.BindVisibility(_segmentRectVM, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem, bindings);
    _segmentRectS.MediaItemSegmentsRects.Bind(_updateSegmentRects).DisposeWith(bindings);
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

  private void _updateSegmentRects(ObservableCollection<SegmentRectM>? col, NotifyCollectionChangedEventArgs e) {
    switch (e.Action) {
      case NotifyCollectionChangedAction.Add:
        if (e.NewItems == null) break;

        foreach (var item in e.NewItems)
          AddView(new SegmentRectV(Context!, (SegmentRectM)item));

        break;

      case NotifyCollectionChangedAction.Remove:
        if (e.OldItems == null) break;

        foreach (var item in e.OldItems) {
          for (int i = ChildCount - 1; i >= 0; i--) {
            if (GetChildAt(i) is not SegmentRectV child || !ReferenceEquals(item, child.DataContext)) continue;
            RemoveViewAt(i);
            child.Dispose();
          }
        }

        break;

      case NotifyCollectionChangedAction.Reset:
        for (int i = ChildCount - 1; i >= 0; i--) {
          if (GetChildAt(i) is not { } child) continue;
          RemoveViewAt(i);
          child.Dispose();
        }

        if (col == null) break;

        foreach (var item in col)
          AddView(new SegmentRectV(Context!, item));

        break;
    }
  }
}