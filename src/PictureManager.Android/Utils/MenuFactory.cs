using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
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
using System.Collections.Generic;

namespace PictureManager.Android.Utils;

public static class MenuFactory {
  public static IEnumerable<MenuItem>? GetMenu(object? item) {
    if (item == null) return null;

    return item switch {
      DriveM => _createDriveMenu(item),
      FolderM => _createFolderMenu(item),
      FavoriteFolderM => [],
      PersonTreeCategory => _createPersonTreeCategoryMenu(item),
      PersonM => _createPersonTreeMenu(item),
      FolderKeywordTreeCategory => _createFolderKeywordTreeCategoryMenu(item),
      KeywordTreeCategory => _createKeywordTreeCategoryMenu(item),
      KeywordM => [],
      GeoNameTreeCategory => _createGeoNamesTreeCategoryMenu(item),
      GeoNameM => [],
      ViewerTreeCategory => [],
      ViewerM => [],
      KeywordCategoryGroupM => [],
      PersonCategoryGroupM => [],
      _ => []
    };
  }

  // Drive
  private static IEnumerable<MenuItem> _createDriveMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item)];

  // Folder
  private static IEnumerable<MenuItem> _createFolderMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.ItemRenameCommand, item),
    new MenuItem(TreeCategory.ItemDeleteCommand, item)];

  // Person TreeCategory
  private static IEnumerable<MenuItem> _createPersonTreeCategoryMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.GroupCreateCommand, item)];

  // Person Tree
  private static IEnumerable<MenuItem> _createPersonTreeMenu(object item) => [
    new MenuItem(ToolsTabsVM.OpenPersonTabCommand, item),
    new MenuItem(TreeCategory.ItemRenameCommand, item),
    new MenuItem(TreeCategory.ItemDeleteCommand, item),
    new MenuItem(TreeCategory.ItemMoveToGroupCommand, item),
    new MenuItem(MediaItemVM.LoadByPersonCommand, item),
    new MenuItem(SegmentVM.LoadByPersonCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetAndCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetOrCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetNotCommand, item)];

  // Folder Keywords
  private static IEnumerable<MenuItem> _createFolderKeywordTreeCategoryMenu(object item) => [
    new MenuItem(FolderKeywordsDialog.OpenCommand, item)];

  // Keyword TreeCategory
  private static IEnumerable<MenuItem> _createKeywordTreeCategoryMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.GroupCreateCommand, item)];

  // GeoNames TreeCategory
  private static IEnumerable<MenuItem> _createGeoNamesTreeCategoryMenu(object item) => [
    new MenuItem(CoreVM.GetGeoNamesFromWebCommand, item),
    new MenuItem(GeoNameVM.NewGeoNameFromGpsCommand, item),
    new MenuItem(CoreVM.ReadGeoLocationFromFilesCommand, item)];
}