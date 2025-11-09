using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Layout;
using PictureManager.Common.Utils;

namespace PictureManager.Android.Views.Layout;

public class MainWindowV : LinearLayout {
  public SlidePanelsGridHost SlidePanels { get; }
  public TabControlHost TreeViewCategories { get; }
  public MiddleContentV MiddleContent { get; }
  public MainWindowVM DataContext { get; }

  public MainWindowV(Context context, MainWindowVM dataContext) : base(context) {
    DataContext = dataContext;
    
    TreeViewCategories = new(context, dataContext.TreeViewCategories, _getTreeViewCategoriesView);
    MiddleContent = new(context, Common.Core.VM);
    SlidePanels = new(
      context,
      dataContext.SlidePanelsGrid,
      TreeViewCategories,
      new ToolBarV(context, dataContext),
      new TextView(Context) { Text = "Right Panel" },
      new TextView(context) { Text = "Bottom Panel" },
      MiddleContent);

    AddView(SlidePanels, new LayoutParams(LPU.Match, LPU.Match));

    this.Bind(Common.Core.VM.MediaViewer, x => x.UserInputMode, (v, p) =>
      v.SlidePanels.ViewPager.UserInputEnabled = p == MediaViewerVM.UserInputModes.Disabled);
  }

  private View? _getTreeViewCategoriesView(LinearLayout container, object? item) {
    if (item is not TreeView tv) return null;
    return new TreeViewHost(container.Context!, tv, MenuFactory.GetMenu);
  }
}