using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views.Sections.ToolBarPanels;

public sealed class MediaItemsToolBarPanel : LinearLayout {
  public MediaItemsToolBarPanel(Context context) : base(context) {
    Orientation = Orientation.Horizontal;
    AddView(new IconButton(context).WithCommand(CoreUI.ShareMediaItemsCommand));
    AddView(new IconButton(context).WithCommand(MediaItemVM.CommentCommand));
    AddView(new IconButton(context).WithCommand(CoreVM.RotateMediaItemsCommand));
    AddView(new IconButton(context).WithCommand(MediaItemVM.DeleteCommand));
    AddView(new IconButton(context).WithCommand(MediaItemsViewsVM.AddViewCommand));
    AddView(new IconButton(context).WithCommand(CoreVM.OpenSegmentsViewsCommand));
    // TODO add thumbs scale
  }
}