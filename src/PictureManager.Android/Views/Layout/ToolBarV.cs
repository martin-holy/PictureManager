using Android.Content;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Sections.ToolBarPanels;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views.Layout;

public sealed class ToolBarV(Context context, CoreVM coreVM) : ToolBar(context) {
  private LoopPager? _loopPager;

  public void Init(LoopPager loopPager, BindingScope bindings) {
    _loopPager = loopPager;
    DefaultPanelsKeys = ["common"];

    RegisterPanel("common", () => new CommonToolBarPanel(Context!, coreVM.MainWindow, bindings));
    RegisterPanel("mediaItems", () => new MediaItemsToolBarPanel(Context!, bindings));
    RegisterPanel("mediaItemThumbs", () => new MediaItemThumbsToolBarPanel(Context!, coreVM.MediaItem.Views, bindings));
    RegisterPanel("mediaViewer", () => new MediaViewerToolBarPanel(Context!, coreVM, bindings));

    RegisterType(typeof(MediaViewerVM), ["common", "mediaItems", "mediaViewer"]);
    RegisterType(typeof(MediaItemsViewVM), ["common", "mediaItems", "mediaItemThumbs"]);

    loopPager.PageChanged += _updateToolBar;
    bindings.AddRange([
      coreVM.MainTabs.Bind(nameof(TabControl.Selected), x => x.Selected, x => _updateToolBar(-1), false),
      coreVM.MainWindow.Bind(nameof(MainWindowVM.IsInViewMode), x => x.IsInViewMode, _ => _updateToolBar(-1), false)]);
    
    _updateToolBar(-1);
  }

  private void _updateToolBar(int index) {
    if (index == -1)
      index = _loopPager!.GetCurrentIndex();

    if (index != 1) {
      Activate(null);
      return;
    }

    if (coreVM.MainWindow.IsInViewMode)
      Activate(typeof(MediaViewerVM));
    else
      Activate(coreVM.MainTabs.Selected?.Data?.GetType());
  }
}