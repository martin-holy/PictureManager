using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views.Layout;

public class ToolBarV : LinearLayout {
  public ToolBarV(Context context, MainWindowVM mainWindowVM) : base(context) {
    Orientation = Orientation.Horizontal;
    AddView(new ButtonMenu(Context!, mainWindowVM.MainMenu, mainWindowVM.MainMenu.Icon));

    var logBtn = new IconButton(context);
    BindingU.Bind(logBtn, CoreVM.OpenLogCommand);
    BindingU.Bind(Core.VM.Log.Items, x => x.Count, count => {
      logBtn.Visibility = count > 0 ? ViewStates.Visible : ViewStates.Gone;
    });
    AddView(logBtn);
  }
}