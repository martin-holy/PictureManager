using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views.Layout;

public class ToolBarV : LinearLayout {
  public ToolBarV(Context context, MainWindowVM mainWindowVM) : base(context) {
    Orientation = Orientation.Horizontal;
    AddView(new ButtonMenu(Context!, mainWindowVM.MainMenu, mainWindowVM.MainMenu.Icon));
  }
}