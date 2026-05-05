using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.SlidePanelsGridHost;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils;
using MH.Utils.Disposables;
using MH.Utils.Interfaces;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;

namespace PictureManager.Android.Views.Layout;

public class MainWindowV : FrameLayout {
  private readonly CoreVM _coreVM;
  private readonly TreeViewCategoriesV _treeViewCategories;
  private readonly ToolBarV _toolBarV;
  private readonly ToolsTabsV _toolsTabsV;
  private readonly StatusBarV _statusBarV;
  private readonly MiddleContentV _middleContent;
  private readonly BindingScope _bindings = new();

  public SlidePanelsGridHost SlidePanels { get; }

  public MainWindowV(Context context, CoreVM coreVM) : base(context) {
    _coreVM = coreVM;
    _treeViewCategories = new(context, _coreVM.MainWindow.TreeViewCategories, _bindings);
    _toolBarV = new(context, _coreVM);
    _toolsTabsV = new(context, _coreVM.MainWindow.ToolsTabs);
    _statusBarV = new(context, _coreVM.MainWindow.StatusBar, _bindings);
    _middleContent = new(context, _coreVM, _bindings);

    SlidePanels = new(
      context,
      _coreVM.MainWindow.SlidePanelsGrid,
      _bindings,
      TopAndBottomPanelsPlacement.MiddleOnly,
      _treeViewCategories,
      _toolBarV,
      _toolsTabsV,
      _statusBarV,
      _middleContent);

    AddView(SlidePanels, LPU.FrameMatch());

    _toolBarV.Init(SlidePanels.ViewPager, _bindings);
    SlidePanels.ViewPager.PageChanged += _onPanelChanged;
    _coreVM.MainWindow.ToolsTabs
      .Bind(nameof(TabControl.Selected), x => x.Selected, _onToolsTabChange, false)
      .DisposeWith(_bindings);
    _middleContent.MediaViewer.ContentTapped += _onMediaViewerContentTapped;
  }

  private void _onMediaViewerContentTapped() {
    _coreVM.MainWindow.SlidePanelsGrid.PanelTop.IsOpen = true;
    _coreVM.MainWindow.SlidePanelsGrid.PanelBottom.IsOpen = true;
  }

  private void _onPanelChanged(int index) {
    if (index == 1 && _coreVM.MainWindow.IsInViewMode)
      _middleContent.MediaViewer.ActivateCurrentPage();
    else if (index == 2)
      _onToolsTabChange(_toolsTabsV.DataContext.Selected);
    else
      _coreVM.Video.Stop();

    UpdateTopAndBottomPanels(_coreVM.MainWindow.SlidePanelsGrid, _coreVM.MainWindow.IsInViewMode, index == 1);
  }

  private void _onToolsTabChange(IListItem? tab) {
    if (SlidePanels.ViewPager.GetCurrentIndex() == 2 && _toolsTabsV.GetTabView(tab) is VideoDetailV videoDetailV)
      videoDetailV.Activate();
    else
      _coreVM.Video.Stop();
  }

  public static void UpdateTopAndBottomPanels(SlidePanelsGrid grid, bool isInViewMode, bool isMiddleActive) {
    grid.PanelTop.IsPinned = !isInViewMode;
    if (!isMiddleActive) return;
    grid.PanelTop.IsOpen = true;
    if (isInViewMode) grid.PanelBottom.IsOpen = true;
  }

  protected override void Dispose(bool disposing) {
    if (disposing) _bindings.Dispose();
    base.Dispose(disposing);
  }
}