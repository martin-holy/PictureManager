using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.FavoriteFolder;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.FolderKeyword;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Rating;
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
      FavoriteFolderM => _createFavoriteFolderMenu(item),
      PersonTreeCategory => _createPersonTreeCategoryMenu(item),
      PersonM => _createPersonTreeMenu(item),
      FolderKeywordTreeCategory => _createFolderKeywordTreeCategoryMenu(item),
      KeywordTreeCategory => _createKeywordTreeCategoryMenu(item),
      KeywordM => _createKeywordTreeMenu(item),
      GeoNameTreeCategory => _createGeoNamesTreeCategoryMenu(item),
      GeoNameM => _createGeoNameTreeMenu(item),
      ViewerTreeCategory => _createViewerTreeCategoryMenu(item),
      ViewerM => _createViewerTreeMenu(item),
      KeywordCategoryGroupM => _createKeywordCategoryGroupMenu(item),
      PersonCategoryGroupM => _createPersonCategoryGroupMenu(item),
      PersonDetailVM pd => _createPersonDetailMenu(pd),
      RatingTreeM r => _createRatingTreeMenu(r),
      SegmentsDrawerVM drawer => _createSegmentsDrawerMenu(drawer),
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
      new(MediaItemVM.CopySelectedToFolderCommand, item),
      new(MediaItemVM.MoveSelectedToFolderCommand, item),
      new(CoreVM.CompressImagesCommand, item),
      new(MediaItemsViewsVM.RebuildThumbnailsCommand, item),
      new(CoreVM.ReloadMetadataCommand, item),
      new(CoreVM.ResizeImagesInFolderCommand, item),
      new(CoreVM.ResizeImagesToFolderCommand, item),
      new(CoreVM.SaveImageMetadataToFilesCommand, item)]),
    new(Res.IconSegment, "Segments", [
      new(CoreVM.ExportSegmentsToCommand, item)])];

  // Favorite Folder
  private static IEnumerable<MenuItem> _createFavoriteFolderMenu(object item) => [
    new(TreeCategory.ItemRenameCommand, item),
    new(TreeCategory.ItemDeleteCommand, item)];

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

  // Keyword tree
  private static IEnumerable<MenuItem> _createKeywordTreeMenu(object item) => [
    new(TreeCategory.ItemCreateCommand, item),
    new(TreeCategory.ItemRenameCommand, item),
    new(TreeCategory.ItemDeleteCommand, item),
    new(TreeCategory.ItemMoveToGroupCommand, item),
    new(MediaItemVM.LoadByKeywordCommand, item),
    new(PersonVM.LoadByKeywordCommand, item),
    new(SegmentVM.LoadByKeywordCommand, item),
    new(MediaItemsViewsVM.FilterSetAndCommand, item),
    new(MediaItemsViewsVM.FilterSetOrCommand, item),
    new(MediaItemsViewsVM.FilterSetNotCommand, item)];

  // GeoNames TreeCategory
  private static IEnumerable<MenuItem> _createGeoNamesTreeCategoryMenu(object item) => [
    new(CoreVM.GetGeoNamesFromWebCommand, item),
    new(GeoNameVM.NewGeoNameFromGpsCommand, item),
    new(CoreVM.ReadGeoLocationFromFilesCommand, item)];

  // GeoName Tree
  private static IEnumerable<MenuItem> _createGeoNameTreeMenu(object item) => [
    new(MediaItemVM.LoadByGeoNameCommand, item),
    new(MediaItemsViewsVM.FilterSetAndCommand, item),
    new(MediaItemsViewsVM.FilterSetOrCommand, item),
    new(MediaItemsViewsVM.FilterSetNotCommand, item)];

  // Viewer TreeCategory
  private static IEnumerable<MenuItem> _createViewerTreeCategoryMenu(object item) => [
    new(TreeCategory.ItemCreateCommand, item)];

  // Viewer tree
  private static IEnumerable<MenuItem> _createViewerTreeMenu(object item) => [
    new(TreeCategory.ItemRenameCommand, item),
    new(TreeCategory.ItemDeleteCommand, item)];

  // Keyword Category Group
  private static IEnumerable<MenuItem> _createKeywordCategoryGroupMenu(object item) => [
    new(TreeCategory.ItemCreateCommand, item),
    new(TreeCategory.GroupRenameCommand, item),
    new(TreeCategory.GroupDeleteCommand, item)];

  // Person Category Group
  private static IEnumerable<MenuItem> _createPersonCategoryGroupMenu(object item) => [
    new(TreeCategory.ItemCreateCommand, item),
    new(TreeCategory.GroupRenameCommand, item),
    new(TreeCategory.GroupDeleteCommand, item),
    new(TreeCategory.GroupMoveInItemsCommand, item),
    new(PersonVM.LoadByCategoryGroupCommand, item)];

  // PersonDetail
  private static IEnumerable<MenuItem> _createPersonDetailMenu(PersonDetailVM item) => [
    new(MediaItemVM.LoadByPersonCommand, item.PersonM),
    new(TreeCategory.ItemRenameCommand, item.PersonM),
    new(SegmentVM.SetSelectedAsUnknownCommand),
    new(SegmentVM.AddSelectedToPersonsTopSegmentsCommand, item.PersonM),
    new(SegmentVM.RemoveSelectedFromPersonsTopSegmentsCommand, item.PersonM)
  ];

  // RatingTree
  private static IEnumerable<MenuItem> _createRatingTreeMenu(RatingTreeM item) => [
    new(MediaItemsViewsVM.LoadByTagCommand, item),
    new(MediaItemsViewsVM.FilterSetOrCommand, item.Rating)
  ];

  // SegmentsDrawer
  private static IEnumerable<MenuItem> _createSegmentsDrawerMenu(SegmentsDrawerVM drawer) => [
    new(SegmentsDrawerVM.AddSelectedCommand),
    new(SegmentsDrawerVM.RemoveSelectedCommand)
  ];
}