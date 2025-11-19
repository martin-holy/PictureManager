using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Views.Layout;

public sealed class ToolsTabsV(Context context, TabControl dataContext) : TabControlHost(context, dataContext) {
  protected override View? _getItemView(LinearLayout container, object? item) =>
    item switch {
      PersonDetailVM personDetail => new PersonDetailV(container.Context!, personDetail),
      _ => null
    };
}