using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils.Disposables;
using PictureManager.Common.Features.Common;

namespace PictureManager.Android.Views.Dialogs;

public sealed class AboutDialogV : LinearLayout {
  public AboutDialogV(Context context, AboutDialog dataContext, BindingScope bindings) : base(context) {
    Orientation = Orientation.Vertical;

    var appName = new TextView(context) { Text = Resources!.GetString(Resource.String.app_name, null), TextSize = 26 };
    var version = new TextView(context) { Text = dataContext.Version };
    var copyright = new TextView(context) { Text = "Martin Holý (2015 - 2026)" }; // TODO decide what is the best multiplatform way to get Copyright :)
    var homePageUrl = new TextView(context) { Text = dataContext.HomePageUrl }
      .WithTextColor(Resource.Color.c_accent)
      .WithClickCommand(dataContext.OpenHomePageCommand, bindings);
    var telegramUrl = new TextView(context) { Text = dataContext.TelegramUrl }
      .WithTextColor(Resource.Color.c_accent)
      .WithClickCommand(dataContext.OpenTelegramCommand, bindings);

    _addViews(this, [appName, version, copyright, homePageUrl, telegramUrl]);
  }

  private static void _addViews(LinearLayout layout, View[] views) {
    foreach (View view in views)
      layout.AddView(view, LPU.LinearWrap().WithMargin(DimensU.Spacing));
  }
}