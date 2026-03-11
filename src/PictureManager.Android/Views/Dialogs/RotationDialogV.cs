using Android.Content;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils.Disposables;
using PictureManager.Common.Features.MediaItem;
using System.Windows.Input;

namespace PictureManager.Android.Views.Dialogs;

public sealed class RotationDialogV : LinearLayout {
  public RotationDialogV(Context context, RotationDialog dataContext, BindingScope bindings) : base(context) {
    Orientation = Orientation.Horizontal;
    var lp = new LayoutParams(DisplayU.DpToPx(64), DisplayU.DpToPx(64)).WithMargin(DimensU.Spacing);

    AddView(_createButton(dataContext.Rotate90Command, bindings), lp);
    AddView(_createButton(dataContext.Rotate180Command, bindings), lp);
    AddView(_createButton(dataContext.Rotate270Command, bindings), lp);
  }

  private ImageView _createButton(ICommand command, BindingScope bindings) {
    var btn = new ImageView(Context)
      .WithClickCommand(command, bindings)
      .WithPadding(DisplayU.DpToPx(20));
    btn.SetBackgroundResource(Resource.Drawable.button_background);
    return btn;
  }
}