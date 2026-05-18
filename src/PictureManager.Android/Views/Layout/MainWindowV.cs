using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.SlidePanelsGridHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils;
using MH.Utils.Disposables;
using MH.Utils.Interfaces;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views.Layout;

public class MainWindowV : FrameLayout {
  private readonly CoreVM _coreVM;
  private readonly TreeViewCategoriesV _treeViewCategories;
  private readonly ToolBarV _toolBarV;
  private readonly ToolsTabsV _toolsTabsV;
  private readonly StatusBarV _statusBarV;
  private readonly MiddleContentV _middleContent;
  private readonly BindingScope _bindings = new();

  public MainWindowVM DataContext { get; }
  public SlidePanelsGridHost SlidePanels { get; }

  public MainWindowV(Context context, CoreVM coreVM) : base(context) {
    _coreVM = coreVM;
    DataContext = _coreVM.MainWindow;

    _treeViewCategories = new(context, DataContext.TreeViewCategories, _bindings);

    _toolBarV = new ToolBarV(context, _coreVM)
      .WithClickAction(_onToolBarClick)
      .WithLongClickAction(_onToolBarLongClick);

    _toolsTabsV = new(context, DataContext.ToolsTabs);

    _statusBarV = new StatusBarV(context, DataContext.StatusBar, _bindings)
      .WithClickAction(_onStatusBarClick)
      .WithLongClickAction(_onStatusBarLongClick);

    _middleContent = new(context, _coreVM, _bindings);

    SlidePanels = new(
      context,
      DataContext.SlidePanelsGrid,
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
    DataContext.ToolsTabs
      .Bind(nameof(TabControl.Selected), x => x.Selected, _onToolsTabChange, false)
      .DisposeWith(_bindings);
    _middleContent.MediaViewer.ContentTapped += _onMediaViewerContentTapped;

    DataContext.Bind(nameof(MainWindowVM.IsFullScreen), x => x.IsFullScreen, _onIsFullScreenChanged, false).DisposeWith(_bindings);
    DataContext.Bind(nameof(MainWindowVM.IsInViewMode), x => x.IsInViewMode, _onIsInViewModeChanged, false).DisposeWith(_bindings);
  }

  private void _onIsFullScreenChanged(bool isFullScreen) {
    if (Context is not MainActivity { Window : { } window }) return;
    if (isFullScreen) window.EnterFullScreen(); else window.ExitFullScreen();
  }

  private void _onIsInViewModeChanged(bool isInViewMode) {
    if (isInViewMode) {
      DataContext.SlidePanelsGrid.PanelTop.IsOpen = true;
      DataContext.SlidePanelsGrid.PanelBottom.IsOpen = true;
    }
  }

  private void _onToolBarClick(ToolBarV _) {
    if (DataContext.IsInViewMode)
      DataContext.SlidePanelsGrid.PanelTop.ToggleOverlay();
  }

  private void _onToolBarLongClick(ToolBarV _) {
    if (DataContext.IsInViewMode)
      DataContext.SlidePanelsGrid.PanelTop.TogglePinned();
  }

  private void _onStatusBarClick(StatusBarV _) {
    if (DataContext.IsInViewMode)
      DataContext.SlidePanelsGrid.PanelBottom.ToggleOverlay();
  }

  private void _onStatusBarLongClick(StatusBarV _) {
    if (DataContext.IsInViewMode)
      DataContext.SlidePanelsGrid.PanelBottom.TogglePinned();
  }

  private void _onMediaViewerContentTapped() {
    DataContext.SlidePanelsGrid.PanelTop.IsOpen = true;
    DataContext.SlidePanelsGrid.PanelBottom.IsOpen = true;
  }

  private void _onPanelChanged(int index) {
    if (index == 1 && DataContext.IsInViewMode)
      _middleContent.MediaViewer.ActivateCurrentPage();
    else if (index == 2)
      _onToolsTabChange(_toolsTabsV.DataContext.Selected);
    else
      _coreVM.Video.Stop();
  }

  private void _onToolsTabChange(IListItem? tab) {
    if (SlidePanels.ViewPager.GetCurrentIndex() == 2 && _toolsTabsV.GetTabView(tab) is VideoDetailV videoDetailV)
      videoDetailV.Activate();
    else
      _coreVM.Video.Stop();
  }

  protected override void Dispose(bool disposing) {
    if (disposing) _bindings.Dispose();
    base.Dispose(disposing);
  }
}