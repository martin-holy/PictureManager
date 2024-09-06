using MH.UI.Controls;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentsViewsTabsSlotVM;

public sealed class SegmentsViewsTabsVM : TabControl {
  public SegmentsViewsTabsVM() : base(new(Dock.Left, Dock.Top, new SegmentsViewsTabsSlotVM())
    { RotationAngle = 270, JustifyTabSize = true }) {
    CanCloseTabs = true;
  }
}