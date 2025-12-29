using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Layout;
using System.Collections;

namespace PictureManager.Android.Views.Layout;

public class ToolBarV : LinearLayout {
  public ToolBarV(Context context, MainWindowVM mainWindowVM) : base(context) {
    Orientation = Orientation.Horizontal;
    AddView(new ButtonMenu(Context!, mainWindowVM.MainMenu, mainWindowVM.MainMenu.Icon));

    AddView(new CompactIconTextButton(context)
      .WithCommand(CoreVM.OpenLogCommand)
      .WithBind(Core.VM.Log.Items, nameof(ICollection.Count), x => x.Count, (view, count) => {
        view.Visibility = count > 0 ? ViewStates.Visible : ViewStates.Gone;
        view.Text.Text = count.ToString();
      }));

    AddView(new CompactIconTextButton(context)
      .WithCommand(CoreVM.SaveDbCommand)
      .WithBind(Core.R, nameof(CoreR.Changes), x => x.Changes, (view, changes) => {
        view.Visibility = changes > 0 ? ViewStates.Visible : ViewStates.Gone;
        view.Text.Text = changes.ToString();
      }));

    AddView(new IconButton(context).WithCommand(CoreUI.ShareMediaItemsCommand));

    _addSegmentRect(context, Core.VM.Segment.Rect);
  }

  private void _addSegmentRect(Context context, SegmentRectVM vm) {
    AddView(new IconToggleButton(context, Res.IconSegmentPerson)
      .BindToggled(vm, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem, (s, p) => s.ShowOverMediaItem = p, out var _));

    AddView(new IconToggleButton(context, Res.IconSegmentEdit)
      .BindToggled(vm, nameof(SegmentRectVM.IsEditEnabled), x => x.IsEditEnabled, (s, p) => s.IsEditEnabled = p, out var _)
      .BindVisibility(vm, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem));

    AddView(new IconToggleButton(context, Res.IconSegmentNew)
      .BindToggled(vm, nameof(SegmentRectVM.CanCreateNew), x => x.CanCreateNew, (s, p) => s.CanCreateNew = p, out var _)
      .BindVisibility(vm, nameof(SegmentRectVM.ShowOverMediaItem), x => x.ShowOverMediaItem && x.IsEditEnabled)
      .BindVisibility(vm, nameof(SegmentRectVM.IsEditEnabled), x => x.ShowOverMediaItem && x.IsEditEnabled));

    AddView(new IconButton(context).WithCommand(SegmentVM.DeleteSelectedCommand));
  }
}