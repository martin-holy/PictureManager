using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views.Layout;

public class MainWindowV : LinearLayout {
  public SlidePanelsGridHost SlidePanels { get; }
  public TreeViewCategoriesV TreeViewCategories { get; }
  public MiddleContentV MiddleContent { get; }
  public MainWindowVM DataContext { get; }

  public MainWindowV(Context context, MainWindowVM dataContext) : base(context) {
    DataContext = dataContext;
    
    TreeViewCategories = new(context, dataContext.TreeViewCategories);
    MiddleContent = new(context, Common.Core.VM);
    SlidePanels = new(
      context,
      dataContext.SlidePanelsGrid,
      TreeViewCategories,
      new ToolBarV(context, dataContext),
      new ToolsTabsV(context, dataContext.ToolsTabs),
      new TextView(context) { Text = "Bottom Panel" },
      MiddleContent);

    AddView(SlidePanels, new LayoutParams(LPU.Match, LPU.Match));
  }
}