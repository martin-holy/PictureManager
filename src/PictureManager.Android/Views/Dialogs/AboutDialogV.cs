using Android.Content;
using Android.Widget;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Common;

namespace PictureManager.Android.Views.Dialogs;

public sealed class AboutDialogV : LinearLayout {
  public AboutDialogV(Context context, AboutDialog dataContext) : base(context) {
    Orientation = Orientation.Vertical;

    AddView(new TextView(context) { Text = Resources!.GetString(Resource.String.app_name, null), TextSize = 26 },
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    AddView(new TextView(context) { Text = dataContext.Version },
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    // TODO decide what is the best multiplatform way to get Copyright :)
    AddView(new TextView(context) { Text = "Martin Holý (2015 - 2026)" },
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));

    AddView(new TextView(context) { Text = dataContext.HomePageUrl }
      .WithTextColor(Resource.Color.c_accent)
      .WithCommand(dataContext.OpenHomePageCommand),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));

    AddView(new TextView(context) { Text = dataContext.TelegramUrl }
      .WithTextColor(Resource.Color.c_accent)
      .WithCommand(dataContext.OpenTelegramCommand),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
  }
}