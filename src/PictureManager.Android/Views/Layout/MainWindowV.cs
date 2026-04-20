using Android.Content;
using Android.Views;
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
  private readonly MiddleContentV _middleContent;
  private readonly BindingScope _bindings = new();

  public SlidePanelsGridHost SlidePanels { get; }

  public MainWindowV(Context context, CoreVM coreVM) : base(context) {
    _coreVM = coreVM;
    _treeViewCategories = new(context, coreVM.MainWindow.TreeViewCategories, _bindings);
    _toolBarV = new(context, coreVM);
    _toolsTabsV = new(context, coreVM.MainWindow.ToolsTabs);
    _middleContent = new(context, coreVM, _bindings);

    SlidePanels = new(
      context,
      coreVM.MainWindow.SlidePanelsGrid,
      _bindings,
      _treeViewCategories,
      _toolBarV,
      _toolsTabsV,
      new TextView(context) { Text = "Bottom Panel" },
      _middleContent);

    AddView(SlidePanels, LPU.FrameMatch());

    _toolBarV.Init(SlidePanels.ViewPager, _bindings);
    SlidePanels.ViewPager.PageChangedEvent += _onPanelChanged;
    coreVM.MainWindow.ToolsTabs
      .Bind(nameof(TabControl.Selected), x => x.Selected, _onToolsTabChange, false)
      .DisposeWith(_bindings);
  }

  private void _onPanelChanged(object? sender, View? view) {
    if (view is MiddleContentV && _coreVM.MainWindow.IsInViewMode)
      _middleContent.MediaViewer.ActivateCurrentPage();
    else if (view is ToolsTabsV toolsTabsV)
      _onToolsTabChange(toolsTabsV.DataContext.Selected);
    else
      _coreVM.Video.Stop();
  }

  private void _onToolsTabChange(IListItem? tab) {
    var toolsTabsV = SlidePanels.ViewPager.GetCurrentItem() as ToolsTabsV;
    if (toolsTabsV?.GetTabView(tab) is VideoDetailV videoDetailV)
      videoDetailV.Activate();
    else
      _coreVM.Video.Stop();
  }

  protected override void Dispose(bool disposing) {
    if (disposing) _bindings.Dispose();
    base.Dispose(disposing);
  }
}