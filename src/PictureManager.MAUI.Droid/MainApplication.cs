using System;
using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace PictureManager.MAUI.Droid;

[Application]
public class MainApplication : MauiApplication {
  public MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }

  protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}