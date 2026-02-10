using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections.ToolBarPanels;

public sealed class MediaViewerToolBarPanel : LinearLayout {
  public MediaViewerToolBarPanel(Context context, CoreVM coreVM) : base(context) {
    Orientation = Orientation.Horizontal;

    AddView(new IconToggleButton(context, Res.IconSegmentPerson)
      .BindToggled(coreVM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem, (s, p) => s.ShowOverMediaItem = p, out var _));

    AddView(new IconToggleButton(context, Res.IconSegmentEdit)
      .BindToggled(coreVM.Segment.Rect, nameof(SegmentRectVM.IsEditEnabled), x => x.IsEditEnabled, (s, p) => s.IsEditEnabled = p, out var _)
      .BindVisibility(coreVM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem));

    AddView(new IconToggleButton(context, Res.IconSegmentNew)
      .BindToggled(coreVM.Segment.Rect, nameof(SegmentRectVM.CanCreateNew), x => x.CanCreateNew, (s, p) => s.CanCreateNew = p, out var _)
      .BindVisibility(coreVM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem && x.IsEditEnabled)
      .BindVisibility(coreVM.Segment.Rect, nameof(SegmentRectVM.IsEditEnabled), x => x.ShowOverMediaItem && x.IsEditEnabled));

    AddView(new IconButton(context).WithCommand(SegmentVM.DeleteSelectedCommand)
      .BindVisibility(coreVM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem));

    AddView(new IconToggleButton(context, MH.UI.Res.IconExpandRect)
      .BindToggled(coreVM.MediaViewer, nameof(MediaViewerVM.ExpandToFill), x => x.ExpandToFill, (s, p) => s.ExpandToFill = p, out var _));

    AddView(new IconToggleButton(context, MH.UI.Res.IconShrinkRect)
      .BindToggled(coreVM.MediaViewer, nameof(MediaViewerVM.ShrinkToFill), x => x.ShrinkToFill, (s, p) => s.ShrinkToFill = p, out var _));
  }
}