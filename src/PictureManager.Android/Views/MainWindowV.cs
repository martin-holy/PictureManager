using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using PictureManager.Common.Layout;
using System;

namespace PictureManager.Android.Views;

public class MainWindowV : LinearLayout {
  private SlidePanelsGridHost _slidePanels = null!;
  private TabControlHost _treeViewCategories = null!;
  private MiddleContentV _middleContent = null!;

  public MainWindowVM? DataContext { get; private set; }

  public MainWindowV(Context context) : base(context) => _initialize(context);
  public MainWindowV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context);
  protected MainWindowV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!);

  private void _initialize(Context context) {
    _slidePanels = new(context);
    AddView(_slidePanels);

    _treeViewCategories = new(context) { GetItemView = _getTreeViewCategoriesView };
    _middleContent = new(context);

    _slidePanels.SetPanelFactory(position => {
      return position switch {
        0 => _treeViewCategories,
        1 => _middleContent,
        2 => new TextView(context) { Text = "Right Panel" },
        _ => throw new ArgumentOutOfRangeException(nameof(position))
      };
    });

    _slidePanels.SetBottomPanel(new TextView(context) { Text = "Bottom Panel" }, false);
  }

  public MainWindowV Bind(MainWindowVM? dataContext) {
    DataContext = dataContext;
    if (DataContext == null) return this;
    _slidePanels.SetTopPanel(new ButtonMenu(Context!) { Root = DataContext.MainMenu.Root });
    _treeViewCategories.Bind(DataContext.TreeViewCategories);
    _middleContent.Bind(Common.Core.VM.MainTabs);
    return this;
  }

  private View? _getTreeViewCategoriesView(LinearLayout container, object? item) {
    if (item is not TreeView tv) return null;
    return new TreeViewHost(container.Context!) { ViewModel = tv };
  }
}