using System.Collections.Generic;
using PictureManager.Domain;
using PictureManager.ViewModels;

namespace PictureManager.ResourceDictionaries {
  public static class Dictionaries {
    public static Dictionary<object, object> IconNameToBrush = new() {
      { "IconFolder", "ColorBrushFolder" },
      { "IconFolderStar", "ColorBrushFolder" },
      { "IconFolderLock", "ColorBrushFolder" },
      { "IconFolderPuzzle", "ColorBrushFolder" },
      { "IconFolderOpen", "ColorBrushFolder" },
      { "IconTag", "ColorBrushTag" },
      { "IconTagLabel", "ColorBrushTag" },
      { "IconPeople", "ColorBrushPeople" },
      { "IconPeopleMultiple", "ColorBrushPeople" },
      { "IconDrive", "ColorBrushDrive" },
      { "IconDriveError", "ColorBrushDrive" },
      { "IconCd", "ColorBrushDrive" }
    };

    public static Dictionary<object, object> CategoryToBrush = new() {
      { Category.Folders, "ColorBrushFolder" },
      { Category.FavoriteFolders, "ColorBrushFolder" },
      { Category.FolderKeywords, "ColorBrushFolder" },
      { Category.People, "ColorBrushPeople" },
      { Category.Keywords, "ColorBrushTag" }
    };

    public static Dictionary<object, object> CategoryToContextMenu = new() {
      { Category.People, "CatPeopleContextMenu" },
      { Category.FolderKeywords, "CatFolderKeywordsContextMenu" },
      { Category.Keywords, "CatKeywordsContextMenu" },
      { Category.GeoNames, "CatGeoNamesContextMenu" },
      { Category.Viewers, "CatViewersContextMenu" },
      { Category.VideoClips, "CatVideoClipsContextMenu" }
    };

    public static Dictionary<object, object> CategoryToIconName = new() {
      { Category.FavoriteFolders, "IconFolderStar" },
      { Category.Folders, "IconFolder" },
      { Category.Ratings, "IconStar" },
      { Category.MediaItemSizes, "IconRuler" },
      { Category.People, "IconPeopleMultiple" },
      { Category.FolderKeywords, "IconFolderPuzzle" },
      { Category.Keywords, "IconTagLabel" },
      { Category.GeoNames, "IconLocationCheckin" },
      { Category.Viewers, "IconEye" },
      { Category.VideoClips, "IconMovieClapper" }
    };

    public static Dictionary<object, object> DisplayFilterToBrush = new() {
      { DisplayFilter.And, "DisplayFilterAndBrush" },
      { DisplayFilter.Or, "DisplayFilterOrBrush" },
      { DisplayFilter.Not, "DisplayFilterNotBrush" }
    };

    public static Dictionary<object, object> ToolsTabsTypeToTemplate = new() {
      { typeof(VideoClipsVM), "Views.VideoClipsV" },
      { typeof(SegmentsDrawerVM), "Views.SegmentsDrawerV" },
      { typeof(PersonVM), "Views.PersonV" }
    };

    public static Dictionary<object, object> MainTabsTypeToTemplate = new() {
      { typeof(PeopleVM), "Views.PeopleV" },
      { typeof(SegmentsVM), "Views.SegmentsV" },
      { typeof(ViewerVM), "Views.ViewerV" },
      { typeof(ThumbnailsGridVM), "Views.ThumbnailsGridV" }
    };

    public static Dictionary<object, object> MainTabsTypeToIcon = new() {
      { typeof(PeopleVM), "IconPeople" },
      { typeof(SegmentsVM), "IconEquals" },
      { typeof(ViewerVM), "IconEye" },
      { typeof(ThumbnailsGridVM), "IconFolder" }
    };

    public static Dictionary<object, object> MainTabsTypeToIconBrush = new() {
      { typeof(PeopleVM), "ColorBrushPeople" },
      { typeof(SegmentsVM), "ColorBrushWhite" },
      { typeof(ViewerVM), "ColorBrushWhite" },
      { typeof(ThumbnailsGridVM), "ColorBrushFolder" }
    };

    public static Dictionary<object, object> MainTabsTypeToContextMenu = new() {
      { typeof(ThumbnailsGridVM), "ThumbnailsGridContextMenu" }
    };
  }
}
