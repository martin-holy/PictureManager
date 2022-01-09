using System.Collections.Generic;
using PictureManager.Domain;

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
  }
}
