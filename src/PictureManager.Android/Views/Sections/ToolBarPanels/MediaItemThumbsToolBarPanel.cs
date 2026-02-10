using Android.Content;
using Android.Widget;

namespace PictureManager.Android.Views.Sections.ToolBarPanels;

public sealed class MediaItemThumbsToolBarPanel : LinearLayout {
  public MediaItemThumbsToolBarPanel(Context context) : base(context) {
    Orientation = Orientation.Horizontal;

    // TODO add thumbs scale
  }
}