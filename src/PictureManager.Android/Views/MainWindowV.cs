using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using PictureManager.Android.Views.Layout;
using PictureManager.Common.Layout;
using PictureManager.Common.Utils;
using System;

namespace PictureManager.Android.Views;

public class MainWindowV : LinearLayout {
  private bool _disposed;

  public SlidePanelsGridHost SlidePanels { get; }
  public TabControlHost TreeViewCategories { get; }
  public MiddleContentV MiddleContent { get; }
  public MainWindowVM DataContext { get; }

  public MainWindowV(Context context, MainWindowVM dataContext) : base(context) {
    DataContext = dataContext;
    SlidePanels = new(context);
    AddView(SlidePanels);

    TreeViewCategories = new(context, DataContext.TreeViewCategories, _getTreeViewCategoriesView) {
      LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    };

    MiddleContent = new(context, Common.Core.VM, SlidePanels.ViewPager) {
      LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    };

    SlidePanels.SetPanelFactory(position => {
      return position switch {
        0 => TreeViewCategories,
        1 => MiddleContent,
        2 => new TextView(context) { Text = "Right Panel" },
        _ => throw new ArgumentOutOfRangeException(nameof(position))
      };
    });

    SlidePanels.SetTopPanel(new ToolBarV(context, dataContext));
    SlidePanels.SetBottomPanel(new TextView(context) { Text = "Bottom Panel" }, false);
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      SlidePanels?.Dispose();
      TreeViewCategories?.Dispose();
      MiddleContent?.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }

  private View? _getTreeViewCategoriesView(LinearLayout container, object? item) {
    if (item is not TreeView tv) return null;
    return new TreeViewHost(container.Context!, tv, MenuFactory.GetMenu);
  }
}