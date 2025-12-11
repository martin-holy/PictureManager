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

    AddView(new CheckBox(context).BindChecked(Core.VM.Segment.Rect, nameof(SegmentRectVM.ShowOverMediaItem),
      x => x.ShowOverMediaItem, (s, p) => s.ShowOverMediaItem = p, out var _));
  }
}