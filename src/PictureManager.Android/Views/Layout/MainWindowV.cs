using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.SlidePanelsGridHost;
using MH.UI.Android.Utils;
using MH.Utils.Disposables;
using PictureManager.Common;

namespace PictureManager.Android.Views.Layout;

public class MainWindowV : FrameLayout {
  private readonly TreeViewCategoriesV _treeViewCategories;
  private readonly ToolBarV _toolBarV;
  private readonly ToolsTabsV _toolsTabsV;
  private readonly MiddleContentV _middleContent;
  private readonly BindingScope _bindings = new();

  public SlidePanelsGridHost SlidePanels { get; }

  public MainWindowV(Context context, CoreVM coreVM) : base(context) {
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
  }

  protected override void Dispose(bool disposing) {
    if (disposing) _bindings.Dispose();
    base.Dispose(disposing);
  }
}