using Android.Content;
using Android.Widget;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.MediaItem;
using System.Windows.Input;

namespace PictureManager.Android.Views.Dialogs;

public sealed class RotationDialogV : LinearLayout {
  public RotationDialogV(Context context, RotationDialog dataContext) : base(context) {
    Orientation = Orientation.Horizontal;
    var lp = new LayoutParams(DisplayU.DpToPx(64), DisplayU.DpToPx(64)).WithMargin(DimensU.Spacing);

    AddView(_createButton(dataContext.Rotate90Command), lp);
    AddView(_createButton(dataContext.Rotate180Command), lp);
    AddView(_createButton(dataContext.Rotate270Command), lp);
  }

  private ImageView _createButton(ICommand command) {
    var btn = new ImageView(Context)
      .WithCommand(command)
      .WithPadding(DisplayU.DpToPx(20));
    btn.SetBackgroundResource(Resource.Drawable.button_background);
    return btn;
  }
}