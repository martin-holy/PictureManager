using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils;
using PictureManager.Android.Views.Layout;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Layout;
using PictureManager.Common.Utils;
using System;

namespace PictureManager.Android.Views;

public class MainWindowV : LinearLayout {
  public SlidePanelsGridHost SlidePanels { get; }
  public TabControlHost TreeViewCategories { get; }
  public MiddleContentV MiddleContent { get; }
  public MainWindowVM DataContext { get; }

  public MainWindowV(Context context, MainWindowVM dataContext) : base(context) {
    DataContext = dataContext;
    SlidePanels = new(context, _slidePanelsFactory);
    TreeViewCategories = new(context, DataContext.TreeViewCategories, _getTreeViewCategoriesView);
    MiddleContent = new(context, Common.Core.VM);
    SlidePanels.SetTopPanel(new ToolBarV(context, dataContext));
    SlidePanels.SetBottomPanel(new TextView(context) { Text = "Bottom Panel" }, false);
    AddView(SlidePanels, new LayoutParams(LPU.Match, LPU.Match));

    this.Bind(Common.Core.VM.MediaViewer, x => x.UserInputMode, (v, p) =>
      v.SlidePanels.ViewPager.UserInputEnabled = p == MediaViewerVM.UserInputModes.Disabled);
  }

  private View _slidePanelsFactory(int position) =>
    position switch {
      0 => TreeViewCategories,
      1 => MiddleContent,
      2 => new TextView(Context) { Text = "Right Panel" },
      _ => throw new ArgumentOutOfRangeException(nameof(position))
    };

  private View? _getTreeViewCategoriesView(LinearLayout container, object? item) {
    if (item is not TreeView tv) return null;
    return new TreeViewHost(container.Context!, tv, MenuFactory.GetMenu);
  }
}