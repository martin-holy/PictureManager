using Microsoft.Maui.Hosting;

namespace PictureManager.MAUI.Droid;

public static class MauiProgram {
  public static MauiApp CreateMauiApp() {
    var builder = MauiApp.CreateBuilder();

    builder
      .UseSharedMauiApp()
      .ConfigureMauiHandlers(MH.UI.MAUI.Droid.MauiProgram.ConfigureHandlers);

    MH.UI.Android.Utils.Icons.IconNameToColor = Resources.Res.IconToColorDic;

    return builder.Build();
  }
}