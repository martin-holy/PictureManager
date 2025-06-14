using Microsoft.Maui.Hosting;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.MAUI.Droid;

public static class MauiProgram {
  public static MauiApp CreateMauiApp() {
    var builder = MauiApp.CreateBuilder();

    builder
      .UseSharedMauiApp()
      .ConfigureMauiHandlers(MH.UI.MAUI.Droid.MauiProgram.ConfigureHandlers);

    MH.UI.Android.Utils.Init.SetDelegates();
    MH.UI.Android.Utils.Icons.IconNameToColor = Resources.Res.IconToColorDic;
    MediaItemS.ReadMetadata = ViewModels.MediaItemVM.ReadMetadata;

    return builder.Build();
  }
}