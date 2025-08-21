using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.BaseClasses;
using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common;
using PictureManager.Common.Features.FavoriteFolder;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.FolderKeyword;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Features.Viewer;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Utils;

// TODO create just TreeView and attach it to one TreeMenuHost
public static class MenuFactory {
  public static PopupWindow? GetMenu(View parent, object? item) {
    if (item == null) return null;
    var menuVM = new TreeView();

    switch (item) {
      case DriveM: _createDriveMenu(menuVM.RootHolder, item); break;
      case FolderM: _createFolderMenu(menuVM.RootHolder, item); break;
      case FavoriteFolderM: break;
      case PersonTreeCategory: _createPersonTreeCategoryMenu(menuVM.RootHolder, item); break;
      case PersonM: _createPersonTreeMenu(menuVM.RootHolder, item); break;
      case FolderKeywordTreeCategory: _createFolderKeywordTreeCategoryMenu(menuVM.RootHolder, item); break;
      case KeywordTreeCategory: _createKeywordTreeCategoryMenu(menuVM.RootHolder, item); break;
      case KeywordM: break;
      case GeoNameTreeCategory: _createGeoNamesTreeCategoryMenu(menuVM.RootHolder, item); break;
      case GeoNameM: break;
      case ViewerTreeCategory: break;
      case ViewerM: break;
      case KeywordCategoryGroupM: break;
      case PersonCategoryGroupM: break;
    };

    var host = new TreeMenuHost(parent.Context!, menuVM, parent);
    return host.Popup;
  }

  // Drive
  private static void _createDriveMenu(ExtObservableCollection<ITreeItem> root, object item) {
    root.Add(new MenuItem(TreeCategory.ItemCreateCommand, item));
  }

  // Folder
  private static void _createFolderMenu(ExtObservableCollection<ITreeItem> root, object item) {
    root.Add(new MenuItem(TreeCategory.ItemCreateCommand, item));
    root.Add(new MenuItem(TreeCategory.ItemRenameCommand, item));
    root.Add(new MenuItem(TreeCategory.ItemDeleteCommand, item));
  }

  // Person TreeCategory
  private static void _createPersonTreeCategoryMenu(ExtObservableCollection<ITreeItem> root, object item) {
    root.Add(new MenuItem(TreeCategory.ItemCreateCommand, item));
    root.Add(new MenuItem(TreeCategory.GroupCreateCommand, item));
  }

  // Person Tree
  private static void _createPersonTreeMenu(ExtObservableCollection<ITreeItem> root, object item) {
    root.Add(new MenuItem(ToolsTabsVM.OpenPersonTabCommand, item));
    root.Add(new MenuItem(TreeCategory.ItemRenameCommand, item));
    root.Add(new MenuItem(TreeCategory.ItemDeleteCommand, item));
    root.Add(new MenuItem(TreeCategory.ItemMoveToGroupCommand, item));
    root.Add(new MenuItem(MediaItemVM.LoadByPersonCommand, item));
    root.Add(new MenuItem(SegmentVM.LoadByPersonCommand, item));
    root.Add(new MenuItem(MediaItemsViewsVM.FilterSetAndCommand, item));
    root.Add(new MenuItem(MediaItemsViewsVM.FilterSetOrCommand, item));
    root.Add(new MenuItem(MediaItemsViewsVM.FilterSetNotCommand, item));
  }

  // Folder Keywords
  private static void _createFolderKeywordTreeCategoryMenu(ExtObservableCollection<ITreeItem> root, object item) {
    root.Add(new MenuItem(FolderKeywordsDialog.OpenCommand, item));
  }

  // Keyword TreeCategory
  private static void _createKeywordTreeCategoryMenu(ExtObservableCollection<ITreeItem> root, object item) {
    root.Add(new MenuItem(TreeCategory.ItemCreateCommand, item));
    root.Add(new MenuItem(TreeCategory.GroupCreateCommand, item));
  }

  // GeoNames TreeCategory
  private static void _createGeoNamesTreeCategoryMenu(ExtObservableCollection<ITreeItem> root, object item) {
    root.Add(new MenuItem(CoreVM.GetGeoNamesFromWebCommand, item));
    root.Add(new MenuItem(GeoNameVM.NewGeoNameFromGpsCommand, item));
    root.Add(new MenuItem(CoreVM.ReadGeoLocationFromFilesCommand, item));
  }
}