using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using PictureManager.Common.Utils;

namespace PictureManager.Android.Views.Layout;

public sealed class TreeViewCategoriesV(Context context, TabControl dataContext) : TabControlHost(context, dataContext) {
  protected override View? _getItemView(LinearLayout container, object? item) =>
    item is TreeView tv
      ? new TreeViewHost(container.Context!, tv, MenuFactory.GetMenu)
      : null;
}