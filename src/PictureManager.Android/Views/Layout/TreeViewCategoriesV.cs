using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Rating;
using PictureManager.Common.Utils;
using System.Linq;

namespace PictureManager.Android.Views.Layout;

public sealed class TreeViewCategoriesV(Context context, TabControl dataContext) : TabControlHost(context, dataContext) {
  protected override View? _getItemView(LinearLayout container, object? item) {
    if (item is not TreeView tv) return null;

    if (tv.RootHolder.FirstOrDefault() is RatingTreeCategory rating)
      return new TreeViewHost(container.Context!, tv, MenuFactory.GetMenu, new RatingTreeItemViewHolderFactory());

    return new TreeViewHost(container.Context!, tv, MenuFactory.GetMenu);
  }
}