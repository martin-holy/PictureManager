using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common;

namespace PictureManager.Android.Views.Layout;

public class MainWindowV : LinearLayout {
  private readonly TreeViewCategoriesV _treeViewCategories;
  private readonly ToolBarV _toolBarV;
  private readonly ToolsTabsV _toolsTabsV;
  private readonly MiddleContentV _middleContent;

  public SlidePanelsGridHost SlidePanels { get; }

  public MainWindowV(Context context, CoreVM coreVM) : base(context) {
    _treeViewCategories = new(context, coreVM.MainWindow.TreeViewCategories);
    _toolBarV = new ToolBarV(context, coreVM);
    _toolsTabsV = new ToolsTabsV(context, coreVM.MainWindow.ToolsTabs);
    _middleContent = new(context, coreVM);

    SlidePanels = new(
      context,
      coreVM.MainWindow.SlidePanelsGrid,
      _treeViewCategories,
      _toolBarV,
      _toolsTabsV,
      new TextView(context) { Text = "Bottom Panel" },
      _middleContent);

    AddView(SlidePanels, new LayoutParams(LPU.Match, LPU.Match));

    _toolBarV.Init(SlidePanels.ViewPager);
  }
}