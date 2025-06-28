using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Interfaces;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Layout;
using System;

namespace PictureManager.Android.Views;

public class MainWindowV : LinearLayout {
  private SlidePanelsGridHost _slidePanels;
  private TreeViewHost _folders = null!;
  private CollectionViewHost _collectionViewMediaItems = null!;
  private MainWindowVM _dataContext = null!;

  public MainWindowVM DataContext { get => _dataContext; set { _dataContext = value; _bind(value); } }

  // TODO test - delete later
  public Common.Features.Folder.FolderTreeView Folders => Common.Core.R.Folder.Tree;
  public Common.Features.MediaItem.MediaItemsViewVM? MediaItemsTestView => Common.Core.VM.MainTabs.Selected?.Data as Common.Features.MediaItem.MediaItemsViewVM;

  // TODO test - delete later
  private void _mainTabs_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
    _collectionViewMediaItems.ViewModel = MediaItemsTestView;
  }

  public MainWindowV(Context context) : base(context) => _initialize(context, null);
  public MainWindowV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context, attrs);
  protected MainWindowV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!, null);

  private void _initialize(Context context, IAttributeSet? attrs) {
    _slidePanels = new SlidePanelsGridHost(context);
    AddView(_slidePanels);

    _folders = new TreeViewHost(context);
    _collectionViewMediaItems = new CollectionViewHost(context);
    _collectionViewMediaItems.GetItemView = _getItemView;

    _slidePanels.SetPanelFactory(position => {
      return position switch {
        0 => _folders,
        1 => _collectionViewMediaItems,
        2 => new TextView(context) { Text = "Right Panel" },
        _ => throw new ArgumentOutOfRangeException(nameof(position))
      };
    });

    _slidePanels.SetBottomPanel(new TextView(context) { Text = "Bottom Panel" }, false);
  }

  private void _bind(MainWindowVM dataContext) {
    _slidePanels.SetTopPanel(new ButtonMenu(Context!) { Root = dataContext.MainMenu.Root });

    _folders.ViewModel = Folders;
    // BUG this is null so I need to set it after select (fake it until you make it)
    //_collectionViewMediaItems.ViewModel = _viewModel.MediaItemsTestView;
    Common.Core.VM.MainTabs.PropertyChanged += _mainTabs_PropertyChanged;
  }

  private View? _getItemView(LinearLayout container, ICollectionViewGroup group, object? item) {
    if (item is not MediaItemM mi) return null;

    return group.GetItemTemplateName() switch {
      "PM.DT.MediaItem.Thumb-Full" => new MediaItemThumbFullV(container.Context!).Bind(mi, MediaItemsTestView, group),
      _ => null,
    };
  }
}