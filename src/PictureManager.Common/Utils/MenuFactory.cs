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

namespace PictureManager.Common.Utils;

public static class MenuFactory {
  public static IEnumerable<MenuItem> GetMenu(object item) =>
    item switch {
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

  // Drive
  private static IEnumerable<MenuItem> _createDriveMenu(object item) => [
    new(TreeCategory.ItemCreateCommand, item)];

  // Folder
  private static IEnumerable<MenuItem> _createFolderMenu(object item) => [
    new(TreeCategory.ItemCreateCommand, item),
    new(TreeCategory.ItemRenameCommand, item),
    new(TreeCategory.ItemDeleteCommand, item),
    new(FavoriteFolderTreeCategory.AddToFavoritesCommand, item),
    new(FolderKeywordTreeCategory.SetAsFolderKeywordCommand, item),
    new(Res.IconLocationCheckin, "GeoLocation", [
      new(CoreVM.GetGeoNamesFromWebCommand, item),
      new(CoreVM.ReadGeoLocationFromFilesCommand, item)]),
    new(Res.IconImageMultiple, "Media Items", [
      new(CoreVM.CompressImagesCommand, item),
      new(MediaItemsViewsVM.RebuildThumbnailsCommand, item),
      new(CoreVM.ReloadMetadataCommand, item),
      new(CoreVM.ResizeImagesCommand, item),
      new(CoreVM.SaveImageMetadataToFilesCommand, item)])];

  // Person TreeCategory
  private static IEnumerable<MenuItem> _createPersonTreeCategoryMenu(object item) => [
    new(TreeCategory.ItemCreateCommand, item),
    new(TreeCategory.GroupCreateCommand, item)];

  // Person Tree
  private static IEnumerable<MenuItem> _createPersonTreeMenu(object item) => [
    new(ToolsTabsVM.OpenPersonTabCommand, item),
    new(TreeCategory.ItemRenameCommand, item),
    new(TreeCategory.ItemDeleteCommand, item),
    new(TreeCategory.ItemMoveToGroupCommand, item),
    new(MediaItemVM.LoadByPersonCommand, item),
    new(SegmentVM.LoadByPersonCommand, item),
    new(MediaItemsViewsVM.FilterSetAndCommand, item),
    new(MediaItemsViewsVM.FilterSetOrCommand, item),
    new(MediaItemsViewsVM.FilterSetNotCommand, item)];

  // Folder Keywords
  private static IEnumerable<MenuItem> _createFolderKeywordTreeCategoryMenu(object item) => [
    new(FolderKeywordsDialog.OpenCommand, item)];

  // Keyword TreeCategory
  private static IEnumerable<MenuItem> _createKeywordTreeCategoryMenu(object item) => [
    new(TreeCategory.ItemCreateCommand, item),
    new(TreeCategory.GroupCreateCommand, item)];

  // GeoNames TreeCategory
  private static IEnumerable<MenuItem> _createGeoNamesTreeCategoryMenu(object item) => [
    new(CoreVM.GetGeoNamesFromWebCommand, item),
    new(GeoNameVM.NewGeoNameFromGpsCommand, item),
    new(CoreVM.ReadGeoLocationFromFilesCommand, item)];
}