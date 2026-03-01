using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
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
  public static IEnumerable<ITreeItem> GetMenu(object item) =>
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
  private static IEnumerable<ITreeItem> _createDriveMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item)];

  // Folder
  private static IEnumerable<ITreeItem> _createFolderMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.ItemRenameCommand, item),
    new MenuItem(TreeCategory.ItemDeleteCommand, item),
    new MenuItem(FavoriteFolderTreeCategory.AddToFavoritesCommand, item),
    new MenuItem(FolderKeywordTreeCategory.SetAsFolderKeywordCommand, item),
    new MenuItem(Res.IconLocationCheckin, "GeoLocation", [
      new MenuItem(CoreVM.GetGeoNamesFromWebCommand, item),
      new MenuItem(CoreVM.ReadGeoLocationFromFilesCommand, item)]),
    new MenuItem(Res.IconImageMultiple, "Media Items", [
      new MenuItem(MediaItemVM.CopySelectedToFolderCommand, item),
      new MenuItem(MediaItemVM.MoveSelectedToFolderCommand, item),
      new MenuItem(CoreVM.CompressImagesCommand, item),
      new MenuItem(MediaItemsViewsVM.RebuildThumbnailsCommand, item),
      new MenuItem(CoreVM.ReloadMetadataCommand, item),
      new MenuItem(CoreVM.ResizeImagesInFolderCommand, item),
      new MenuItem(CoreVM.ResizeImagesToFolderCommand, item),
      new MenuItem(CoreVM.SaveImageMetadataToFilesCommand, item)]),
    new MenuItem(Res.IconSegment, "Segments", [
      new MenuItem(CoreVM.ExportSegmentsToCommand, item)])];

  // Favorite Folder
  private static IEnumerable<ITreeItem> _createFavoriteFolderMenu(object item) => [
    new MenuItem(TreeCategory.ItemRenameCommand, item),
    new MenuItem(TreeCategory.ItemDeleteCommand, item)];

  // Person TreeCategory
  private static IEnumerable<ITreeItem> _createPersonTreeCategoryMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.GroupCreateCommand, item)];

  // Person Tree
  private static IEnumerable<ITreeItem> _createPersonTreeMenu(object item) => [
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
  private static IEnumerable<ITreeItem> _createFolderKeywordTreeCategoryMenu(object item) => [
    new MenuItem(FolderKeywordsDialog.OpenCommand, item)];

  // Keyword TreeCategory
  private static IEnumerable<ITreeItem> _createKeywordTreeCategoryMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.GroupCreateCommand, item)];

  // Keyword tree
  private static IEnumerable<ITreeItem> _createKeywordTreeMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.ItemRenameCommand, item),
    new MenuItem(TreeCategory.ItemDeleteCommand, item),
    new MenuItem(TreeCategory.ItemMoveToGroupCommand, item),
    new MenuItem(MediaItemVM.LoadByKeywordCommand, item),
    new MenuItem(PersonVM.LoadByKeywordCommand, item),
    new MenuItem(SegmentVM.LoadByKeywordCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetAndCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetOrCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetNotCommand, item)];

  // GeoNames TreeCategory
  private static IEnumerable<ITreeItem> _createGeoNamesTreeCategoryMenu(object item) => [
    new MenuItem(CoreVM.GetGeoNamesFromWebCommand, item),
    new MenuItem(GeoNameVM.NewGeoNameFromGpsCommand, item),
    new MenuItem(CoreVM.ReadGeoLocationFromFilesCommand, item)];

  // GeoName Tree
  private static IEnumerable<ITreeItem> _createGeoNameTreeMenu(object item) => [
    new MenuItem(MediaItemVM.LoadByGeoNameCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetAndCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetOrCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetNotCommand, item)];

  // Viewer TreeCategory
  private static IEnumerable<ITreeItem> _createViewerTreeCategoryMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item)];

  // Viewer tree
  private static IEnumerable<ITreeItem> _createViewerTreeMenu(object item) => [
    new MenuItem(TreeCategory.ItemRenameCommand, item),
    new MenuItem(TreeCategory.ItemDeleteCommand, item)];

  // Keyword Category Group
  private static IEnumerable<ITreeItem> _createKeywordCategoryGroupMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.GroupRenameCommand, item),
    new MenuItem(TreeCategory.GroupDeleteCommand, item)];

  // Person Category Group
  private static IEnumerable<ITreeItem> _createPersonCategoryGroupMenu(object item) => [
    new MenuItem(TreeCategory.ItemCreateCommand, item),
    new MenuItem(TreeCategory.GroupRenameCommand, item),
    new MenuItem(TreeCategory.GroupDeleteCommand, item),
    new MenuItem(TreeCategory.GroupMoveInItemsCommand, item),
    new MenuItem(PersonVM.LoadByCategoryGroupCommand, item)];

  // PersonDetail
  private static IEnumerable<ITreeItem> _createPersonDetailMenu(PersonDetailVM item) => [
    new MenuItem(MediaItemVM.LoadByPersonCommand, item.PersonM),
    new MenuItem(TreeCategory.ItemRenameCommand, item.PersonM),
    new MenuItem(SegmentVM.SetSelectedAsUnknownCommand),
    new MenuItem(SegmentVM.AddSelectedToPersonsTopSegmentsCommand, item.PersonM),
    new MenuItem(SegmentVM.RemoveSelectedFromPersonsTopSegmentsCommand, item.PersonM)
  ];

  // RatingTree
  private static IEnumerable<ITreeItem> _createRatingTreeMenu(RatingTreeM item) => [
    new MenuItem(MediaItemsViewsVM.LoadByTagCommand, item),
    new MenuItem(MediaItemsViewsVM.FilterSetOrCommand, item.Rating)
  ];

  // SegmentsDrawer
  private static IEnumerable<ITreeItem> _createSegmentsDrawerMenu(SegmentsDrawerVM drawer) => [
    new MenuItem(SegmentsDrawerVM.AddSelectedCommand),
    new MenuItem(SegmentsDrawerVM.RemoveSelectedCommand)
  ];
}