﻿using Android.Content;
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
  public SlidePanelsGridHost SlidePanels { get; private set; } = null!;
  public TabControlHost TreeViewCategories { get; private set; } = null!;
  public MiddleContentV MiddleContent { get; private set; } = null!;
  public MainWindowVM? DataContext { get; private set; }

  public MainWindowV(Context context) : base(context) => _initialize(context);
  public MainWindowV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context);
  protected MainWindowV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!);

  private void _initialize(Context context) {
    SlidePanels = new(context);
    AddView(SlidePanels);

    TreeViewCategories = new(context) {
      LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
      GetItemView = _getTreeViewCategoriesView
    };

    MiddleContent = new(context) {
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

    SlidePanels.SetBottomPanel(new TextView(context) { Text = "Bottom Panel" }, false);
  }

  public MainWindowV Bind(MainWindowVM? dataContext) {
    DataContext = dataContext;
    if (DataContext == null) return this;
    SlidePanels.SetTopPanel(new ButtonMenu(Context!) { Root = DataContext.MainMenu.Root });
    TreeViewCategories.Bind(DataContext.TreeViewCategories);
    MiddleContent.Bind(Common.Core.VM, SlidePanels.ViewPager);
    return this;
  }

  private View? _getTreeViewCategoriesView(LinearLayout container, object? item) {
    if (item is not TreeView tv) return null;
    return new TreeViewHost(container.Context!).Bind(tv);
  }
}