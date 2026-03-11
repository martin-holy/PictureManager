using Android.Content;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.Utils.Disposables;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views.Sections.ToolBarPanels;

public sealed class MediaItemsToolBarPanel : LinearLayout {
  public MediaItemsToolBarPanel(Context context, BindingScope bindings) : base(context) {
    Orientation = Orientation.Horizontal;

    AddView(new IconButton(context).WithClickCommand(CoreUI.ShareMediaItemsCommand, bindings));
    AddView(new IconButton(context).WithClickCommand(MediaItemVM.DeleteCommand, bindings));
  }
}