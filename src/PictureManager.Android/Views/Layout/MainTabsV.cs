using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Views;
using MH.UI.Controls;
using MH.UI.ViewModels;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views.Layout;

public sealed class MainTabsV(Context context, TabControl dataContext) : TabControlHost(context, dataContext) {
  protected override View? _getItemView(LinearLayout container, object? item) =>
    item switch {
      AllSettings allSettings => new SettingsV(container.Context!, allSettings),
      MediaItemsViewVM miv => new MediaItemsViewV(container.Context!, miv),
      LogVM log => new LogV(container.Context!, log),
      View view => view,
      _ => null
    };
}