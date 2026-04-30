using Android.Content;
using Android.Widget;

namespace PictureManager.Android.Views.Layout;

public sealed class StatusBarV : LinearLayout {
  public StatusBarV(Context? context, Common.Layout.StatusBarVM statusBar) : base(context) {
    AddView(new TextView(context) { Text = "StatusBar" });
  }
}