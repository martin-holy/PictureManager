using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Interfaces;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Layout;

namespace PictureManager.MAUI.Droid.Views;

public class MainWindowV : LinearLayout {
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
    LayoutInflater.From(context)!.Inflate(Resource.Layout.main_window, this, true);
    _folders = FindViewById<TreeViewHost>(Resource.Id.tree_view_folders)!;
    _collectionViewMediaItems = FindViewById<CollectionViewHost>(Resource.Id.collection_view_media_items)!;
    _collectionViewMediaItems.GetItemView = _getItemView;
  }

  private void _bind(MainWindowVM dataContext) {
    _folders.Bind(Folders);
    // BUG this is null so I need to set it after select (fake it until you make it)
    //_collectionViewMediaItems.ViewModel = _viewModel.MediaItemsTestView;
    Common.Core.VM.MainTabs.PropertyChanged += _mainTabs_PropertyChanged;
  }

  private View? _getItemView(LinearLayout container, ICollectionViewGroup group, object? item) {
    if (item is not MediaItemM mi) return null;

    return group.GetItemTemplateName() switch {
      "PM.DT.MediaItem.Thumb-Full" => new MediaItemThumbFullV(container.Context!) { DataContext = mi },
      _ => null,
    };
  }
}