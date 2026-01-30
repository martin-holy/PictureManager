using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using PictureManager.Android.Views.Entities;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Rating;
using PictureManager.Common.Utils;
using System;
using System.Linq;

namespace PictureManager.Android.Views.Layout;

public sealed class TreeViewCategoriesV : FrameLayout {
  private readonly TreeViewCategoriesVM _dataContext;
  private readonly TabsV _tabsV;
  private readonly TreeViewSearchV _searchV;

  public TreeViewCategoriesV(Context context, TreeViewCategoriesVM dataContext) : base(context) {
    _dataContext = dataContext;
    _tabsV = new TabsV(context, dataContext, _slotFactory);
    _searchV = new TreeViewSearchV(context, dataContext.TreeViewSearch);
    _searchV.BindVisibility(dataContext.TreeViewSearch, nameof(TreeViewSearchVM.IsOpen), x => x.IsOpen);

    AddView(_tabsV, new LayoutParams(LPU.Match, LPU.Match));
    AddView(_searchV, new LayoutParams(LPU.Match, LPU.Match));
  }

  private TreeViewCategoriesSlotV? _slotFactory(object? o) =>
    o is TreeViewCategoriesSlotVM ? new(Context!, _dataContext.TreeViewSearch) : null;

  private sealed class TreeViewCategoriesSlotV : FrameLayout {
    public TreeViewCategoriesSlotV(Context context, TreeViewSearchVM treeViewSearchVM) : base(context) {
      AddView(new IconButton(context).WithCommand(treeViewSearchVM.OpenCommand));
    }
  }

  private sealed class TabsV(Context context, TabControl dataContext, Func<object?, View?>? slotFactory)
    : TabControlHost(context, dataContext, slotFactory) {

    protected override View? _getItemView(LinearLayout container, object? item) {
      if (item is not TreeView tv) return null;

      if (tv.RootHolder.FirstOrDefault() is RatingTreeCategory rating)
        return new TreeViewHost(container.Context!, tv, MenuFactory.GetMenu, new RatingTreeItemViewHolderFactory());

      return new TreeViewHost(container.Context!, tv, MenuFactory.GetMenu);
    }
  }
}