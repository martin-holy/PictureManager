using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;

namespace PictureManager.Android.Views;

public class SplashScreenV : LinearLayout {
  private TextView _version = null!;
  private TextView _message = null!;

  public IProgress<string> ProgressMessage { get; private set; } = null!;

  public SplashScreenV(Context context) : base(context) => _initialize(context, null);
  public SplashScreenV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context, attrs);
  protected SplashScreenV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!, null);

  private void _initialize(Context context, IAttributeSet? attrs) {
    LayoutInflater.From(context)!.Inflate(Resource.Layout.pm_dt_splash_screen, this, true);
    SetBackgroundResource(Resource.Color.c_static_ba);
    SetGravity(GravityFlags.Top);
    var screenHeight = Resources!.DisplayMetrics!.HeightPixels;
    SetPadding(0, (int)(screenHeight * 0.2), 0, 0);
    Orientation = Orientation.Vertical;

    // TODO PORT version from elsewhere
    _version = FindViewById<TextView>(Resource.Id.version)!;
    //_version.Text = $"ver.: {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";

    _message = FindViewById<TextView>(Resource.Id.message)!;
    ProgressMessage = new Progress<string>(msg => _message.SetText(msg, TextView.BufferType.Normal));
  }
}