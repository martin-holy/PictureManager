using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.TabControlHost;
using MH.UI.Android.Controls.Hosts.TreeViewHost;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Entities;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Rating;
using PictureManager.Common.Utils;
using System;

namespace PictureManager.Android.Views.Layout;

public sealed class TreeViewCategoriesV : FrameLayout {
  private readonly TreeViewCategoriesVM _dataContext;
  private readonly BindingScope _bindings;
  private readonly TabsV _tabsV;
  private readonly TreeViewSearchV _searchV;

  public TreeViewCategoriesV(Context context, TreeViewCategoriesVM dataContext, BindingScope bindings) : base(context) {
    _dataContext = dataContext;
    _bindings = bindings;
    _tabsV = new TabsV(context, dataContext, _slotFactory);
    _searchV = new TreeViewSearchV(context, dataContext.TreeViewSearch, bindings);
    _searchV.BindVisibility(dataContext.TreeViewSearch, nameof(TreeViewSearchVM.IsOpen), x => x.IsOpen, bindings);

    AddView(_tabsV, new LayoutParams(LPU.Match, LPU.Match));
    AddView(_searchV, new LayoutParams(LPU.Match, LPU.Match));
  }

  private TreeViewCategoriesSlotV? _slotFactory(object? o) =>
    o is TreeViewCategoriesSlotVM ? new(Context!, _dataContext.TreeViewSearch, _bindings) : null;

  private sealed class TreeViewCategoriesSlotV : FrameLayout {
    public TreeViewCategoriesSlotV(Context context, TreeViewSearchVM treeViewSearchVM, BindingScope bindings) : base(context) {
      AddView(new IconButton(context).WithClickCommand(treeViewSearchVM.OpenCommand, bindings));
    }
  }

  private sealed class TabsV : TabControlHost {
    public TabsV(Context context, TabControl dataContext, Func<object?, View?>? slotFactory)
      : base(context, dataContext, slotFactory) { }

    protected override View? _getItemView(Context context, object? item) {
      if (item is not TreeView tv) return null;

      if (tv.RootHolder.Count > 0 && tv.RootHolder[0] is RatingTreeCategory rating)
        return new TreeViewHost(context, tv, MenuFactory.GetMenu, new RatingTreeItemViewHolderFactory());

      return new TreeViewHost(context, tv, MenuFactory.GetMenu);
    }
  }
}