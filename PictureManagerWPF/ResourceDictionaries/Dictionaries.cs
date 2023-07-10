using System.Collections.Generic;
using PictureManager.Domain;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using PictureManager.ViewModels;

namespace PictureManager.ResourceDictionaries {
  public static class Dictionaries {
    public static readonly Dictionary<object, object> IconNameToBrush = new() {
      { "default", "ColorBrushWhite" },
      { Res.IconFolder, "ColorBrushFolder" },
      { Res.IconFolderStar, "ColorBrushFolder" },
      { Res.IconFolderLock, "ColorBrushFolder" },
      { Res.IconFolderPuzzle, "ColorBrushFolder" },
      { Res.IconFolderOpen, "ColorBrushFolder" },
      { Res.IconTag, "ColorBrushTag" },
      { Res.IconTagLabel, "ColorBrushTag" },
      { Res.IconPeople, "ColorBrushPeople" },
      { Res.IconPeopleMultiple, "ColorBrushPeople" },
      { Res.IconDrive, "ColorBrushDrive" },
      { Res.IconDriveError, "ColorBrushDrive" },
      { Res.IconCd, "ColorBrushDrive" }
    };

    public static readonly Dictionary<object, object> TypeToTreeContextMenu = new() {
      { typeof(DriveM), "TreeContextMenuDrive" },
      { typeof(FolderM), "TreeContextMenuFolder" },
      { typeof(FavoriteFolderM), "TreeContextMenuFavoriteFolder" },
      { typeof(PeopleM), "TreeContextMenuPeople" },
      { typeof(PersonM), "TreeContextMenuPerson" },
      { typeof(FolderKeywordsM), "TreeContextMenuFolderKeywords" },
      { typeof(KeywordsM), "TreeContextMenuKeywords" },
      { typeof(KeywordM), "TreeContextMenuKeyword" },
      { typeof(GeoNamesM), "TreeContextMenuGeoNames" },
      { typeof(GeoNameM), "TreeContextMenuGeoName" },
      { typeof(ViewersM), "TreeContextMenuViewers" },
      { typeof(ViewerM), "TreeContextMenuViewer" },
      { typeof(VideoClipsTreeCategory), "TreeContextMenuVideoClips" },
      { typeof(CategoryGroupM), "TreeContextMenuGroup" },
      { typeof(VideoClipsGroupM), "TreeContextMenuVideoClipsGroup" }
    };

    public static readonly Dictionary<object, object> MainTabsTypeToIcon = new() {
      { typeof(PeopleView), Res.IconPeople },
      { typeof(SegmentsView), Res.IconSegment },
      { typeof(SegmentsVM), Res.IconEquals },
      { typeof(ViewerDetailM), Res.IconEye },
      { typeof(ThumbnailsGridM), Res.IconFolder }
    };

    public static readonly Dictionary<object, object> MainTabsTypeToIconBrush = new() {
      { typeof(PeopleView), "ColorBrushPeople" },
      { typeof(SegmentsView), "ColorBrushWhite" },
      { typeof(SegmentsVM), "ColorBrushWhite" },
      { typeof(ViewerDetailM), "ColorBrushWhite" },
      { typeof(ThumbnailsGridM), "ColorBrushFolder" }
    };

    public static readonly Dictionary<object, object> MainTabsTypeToContextMenu = new() {
      { typeof(ThumbnailsGridM), "ThumbnailsGridContextMenu" }
    };
  }
}
