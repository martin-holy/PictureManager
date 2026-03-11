using Android.Content;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.Utils.Disposables;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections.ToolBarPanels;

public sealed class MediaViewerToolBarPanel : LinearLayout {
  public MediaViewerToolBarPanel(Context context, CoreVM coreVM, BindingScope bindings) : base(context) {
    Orientation = Orientation.Horizontal;

    AddView(new IconToggleButton(context, Res.IconSegmentPerson)
      .BindToggled(coreVM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem, (s, p) => s.ShowOverMediaItem = p, bindings));

    AddView(new IconToggleButton(context, Res.IconSegmentEdit)
      .BindToggled(coreVM.Segment.Rect, nameof(SegmentRectVM.IsEditEnabled), x => x.IsEditEnabled, (s, p) => s.IsEditEnabled = p, bindings)
      .BindVisibility(coreVM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem, bindings));

    AddView(new IconToggleButton(context, Res.IconSegmentNew)
      .BindToggled(coreVM.Segment.Rect, nameof(SegmentRectVM.CanCreateNew), x => x.CanCreateNew, (s, p) => s.CanCreateNew = p, bindings)
      .BindVisibility(coreVM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem && x.IsEditEnabled, bindings)
      .BindVisibility(coreVM.Segment.Rect, nameof(SegmentRectVM.IsEditEnabled), x => x.ShowOverMediaItem && x.IsEditEnabled, bindings));

    AddView(new IconButton(context).WithClickCommand(SegmentVM.DeleteSelectedCommand, bindings)
      .BindVisibility(coreVM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem, bindings));

    AddView(new IconToggleButton(context, MH.UI.Res.IconExpandRect)
      .BindToggled(coreVM.MediaViewer, nameof(MediaViewerVM.ExpandToFill), x => x.ExpandToFill, (s, p) => s.ExpandToFill = p, bindings));

    AddView(new IconToggleButton(context, MH.UI.Res.IconShrinkRect)
      .BindToggled(coreVM.MediaViewer, nameof(MediaViewerVM.ShrinkToFill), x => x.ShrinkToFill, (s, p) => s.ShrinkToFill = p, bindings));
  }
}