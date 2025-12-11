using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.ViewModels;

public sealed class SegmentRectUiVM : ObservableObject {
  public SegmentRectVM SegmentRectVM { get; }
  public SegmentRectS SegmentRectS { get; }

  // called from Android view to know whether user is touching a resize corner or the body
  public enum HitType {
    None,
    Move,
    Resize
  }

  public SegmentRectUiVM(SegmentRectVM segmentRectVM, SegmentRectS segmentRectS) {
    SegmentRectVM = segmentRectVM;
    SegmentRectS = segmentRectS;
  }

  // --------------------------------------------------------------
  //  SELECTION
  // --------------------------------------------------------------

  /// Called when user touches a segment or its resize/move point.
  public void SetCurrent(SegmentRectM? rect, float x, float y, HitType hit) {
    if (rect == null) {
      SegmentRectS.SetCurrent(null, x, y);
      return;
    }

    SegmentRectS.SetCurrent(rect, x, y);

    // record that editing mode begins depending on hit area
    CurrentHit = hit;
  }

  // --------------------------------------------------------------
  //  CREATION
  // --------------------------------------------------------------

  /// Called when touch happens on empty space.
  public void BeginCreate(float x, float y) {
    SegmentRectS.CreateNew(x, y);
    CurrentHit = HitType.Resize; // new segments start by dragging size
  }

  // --------------------------------------------------------------
  //  EDITING (move/resize)
  // --------------------------------------------------------------

  public HitType CurrentHit { get; private set; } = HitType.None;

  /// Called on ACTION_MOVE when finger is down.
  public void UpdateEdit(float x, float y) {
    if (SegmentRectS.Current == null || CurrentHit == HitType.None)
      return;

    SegmentRectS.Edit(x, y);
  }

  /// Called on ACTION_UP
  public void EndEdit() {
    SegmentRectS.EndEdit();
    CurrentHit = HitType.None;
  }

  // --------------------------------------------------------------
  //  DELETE
  // --------------------------------------------------------------

  public void Delete(SegmentRectM rect) {
    SegmentRectS.Delete(rect);
  }
}
