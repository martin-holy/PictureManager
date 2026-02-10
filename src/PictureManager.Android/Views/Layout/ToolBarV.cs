using Android.Content;
using Android.Views;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Android.Views.Sections.ToolBarPanels;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views.Layout;

public sealed class ToolBarV : ToolBar {
  private readonly CoreVM _coreVM;
  private LoopPager? _loopPager;

  public ToolBarV(Context context, CoreVM coreVM) : base(context) {
    _coreVM = coreVM;
  }

  public void Init(LoopPager loopPager) {
    _loopPager = loopPager;
    DefaultPanelsKeys = ["common"];

    RegisterPanel("common", () => new CommonToolBarPanel(Context!, _coreVM.MainWindow));
    RegisterPanel("mediaItems", () => new MediaItemsToolBarPanel(Context!));
    RegisterPanel("mediaItemThumbs", () => new MediaItemThumbsToolBarPanel(Context!));
    RegisterPanel("mediaViewer", () => new MediaViewerToolBarPanel(Context!, _coreVM));

    RegisterType(typeof(MediaViewerVM), ["common", "mediaItems", "mediaViewer"]);
    RegisterType(typeof(MediaItemsViewVM), ["common", "mediaItems", "mediaItemThumbs"]);

    loopPager.PageChangedEvent += (_, page) => _updateToolBar(null, page);
    this.Bind(_coreVM.MainWindow.TreeViewCategories, nameof(TabControl.Selected), x => x.Selected, (t, p) => t._updateToolBar(p, null), false);
    this.Bind(_coreVM.MainTabs, nameof(TabControl.Selected), x => x.Selected, (t, p) => t._updateToolBar(p, null), false);
    this.Bind(_coreVM.MainWindow.ToolsTabs, nameof(TabControl.Selected), x => x.Selected, (t, p) => t._updateToolBar(p, null), false);
    this.Bind(_coreVM.MediaViewer, nameof(MediaViewerVM.IsVisible), x => x.IsVisible, (t, _) => t._updateToolBar(null, null), false);
    _updateToolBar(null, null);
  }

  private void _updateToolBar(IListItem? tab, View? page) {
    page ??= _loopPager?.GetCurrentItem();

    if (_coreVM.MediaViewer.IsVisible && page is MiddleContentV) {
      Activate(typeof(MediaViewerVM));
      return;
    }

    tab ??= _pageToSelectedTab(page);
    Activate(tab?.Data?.GetType());
  }

  private IListItem? _pageToSelectedTab(View? page) =>
    page switch {
      TreeViewCategoriesV => _coreVM.MainWindow.TreeViewCategories.Selected,
      MiddleContentV => _coreVM.MainTabs.Selected,
      ToolsTabsV => _coreVM.ToolsTabs.Selected,
      _ => null
    };
}