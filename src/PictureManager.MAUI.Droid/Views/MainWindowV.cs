using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using PictureManager.Common.Layout;

namespace PictureManager.MAUI.Droid.Views;

public class MainWindowV : LinearLayout {
  private TreeViewHost _folders = null!;
  private CollectionViewHost _collectionViewMediaItems = null!;
  private MainWindowVM _viewModel = null!;

  public MainWindowVM ViewModel {
    get => _viewModel;
    set {
      _viewModel = value;
      _folders.ViewModel = Folders;
      // BUG this is null so I need to set it after select (fake it until you make it)
      //_collectionViewMediaItems.ViewModel = _viewModel.MediaItemsTestView;
      Common.Core.VM.MainTabs.PropertyChanged += _mainTabs_PropertyChanged;
    }
  }

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
  }
}