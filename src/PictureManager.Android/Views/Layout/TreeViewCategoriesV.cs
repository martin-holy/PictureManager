using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.TabControlHost;
using MH.UI.Android.Controls.Hosts.TreeViewHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Entities;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Rating;
using PictureManager.Common.Layout;
using PictureManager.Common.Utils;

namespace PictureManager.Android.Views.Layout;

public sealed class TreeViewCategoriesV : FrameLayout {
  private readonly TreeViewCategoriesVM _dataContext;
  private readonly TabsV _tabsV;
  private readonly TreeViewSearchV _searchV;

  public TreeViewCategoriesV(Context context, TreeViewCategoriesVM dataContext, BindingScope bindings) : base(context) {
    _dataContext = dataContext;
    _tabsV = new TabsV(context, _dataContext, bindings);
    _searchV = new TreeViewSearchV(context, _dataContext.TreeViewSearch, bindings);
    _searchV.BindVisibility(_dataContext.TreeViewSearch, nameof(TreeViewSearchVM.IsOpen), x => x.IsOpen, bindings);

    AddView(_tabsV, LPU.FrameMatch());
    AddView(_searchV, LPU.FrameMatch());
  }

  private sealed class TreeViewCategoriesSlotV : FrameLayout {
    public TreeViewCategoriesSlotV(Context context, TreeViewSearchVM treeViewSearchVM, BindingScope bindings) : base(context) {
      AddView(new IconButton(context).WithClickCommand(treeViewSearchVM.OpenCommand, bindings), LPU.FrameWrap()
        .WithMargin(0, DimensU.Spacing, DimensU.Spacing, 0));
    }
  }

  private sealed class MainMenuV : FrameLayout {
    public MainMenuV(Context context, MainMenuVM mainMenuVM) : base(context) {
      AddView(new ButtonMenu(context, mainMenuVM, mainMenuVM.Icon), LPU.FrameWrap()
        .WithMargin(DimensU.Spacing, DimensU.Spacing, 0, 0));
    }
  }

  private sealed class TabsV : TabControlHost {
    private readonly TreeViewCategoriesVM _dataContext;
    private readonly BindingScope _bindings;

    public TabsV(Context context, TreeViewCategoriesVM dataContext, BindingScope bindings) : base(context, dataContext) {
      _dataContext = dataContext;
      _bindings = bindings;
      _initialize();
    }

    protected override View? _viewFactory(Context context, object? item) {
      if (item is not TreeView tv) return null;

      if (tv.RootHolder.Count > 0 && tv.RootHolder[0] is RatingTreeCategory rating)
        return new TreeViewHost(context, tv, MenuFactory.GetMenu, new RatingTreeItemViewHolderFactory());

      return new TreeViewHost(context, tv, MenuFactory.GetMenu);
    }

    protected override View? _slotFactory(Context context, object? item) =>
      item switch {
        TreeViewCategoriesSlotVM => new TreeViewCategoriesSlotV(context, _dataContext.TreeViewSearch, _bindings),
        MainMenuVM mainMenuVM => new MainMenuV(context, mainMenuVM),
        _ => null
      };
  }
}