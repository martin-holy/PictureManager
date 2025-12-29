using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using PictureManager.Common;
using System;

namespace PictureManager.Android.Views;

public class SplashScreenV : LinearLayout {
  private readonly DisplayMetrics _dm;

  public IProgress<string> ProgressMessage { get; private set; } = null!;

  public SplashScreenV(Context context) : base(context) {
    _dm = Resources!.DisplayMetrics!;
    SetBackgroundResource(Resource.Color.c_static_ba);
    SetGravity(GravityFlags.Center);
    Orientation = Orientation.Vertical;

    var appName = new TextView(context) {
      Text = Resources.GetString(Resource.String.app_name, null),
      TextSize = 28
    };
    appName.SetTypeface(null, TypefaceStyle.Bold);

    var version = new TextView(context) {
      Text = $"Version: {Core.UiVersion} (Core: {Core.Version})"
    };

    var message = new TextView(context);

    ProgressMessage = new Progress<string>(msg => message.Text = msg);

    AddView(appName, new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.CenterHorizontal }.WithMargin(_dpToPx(4)));
    AddView(version, new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.CenterHorizontal }.WithMargin(_dpToPx(4)));
    AddView(message, new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.CenterHorizontal }.WithMargin(_dpToPx(4)));
  }

  private int _dpToPx(float dp) => (int)(dp * _dm.Density);
}