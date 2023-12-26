using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.TreeCategories;
using System.Collections.Generic;

namespace PictureManager.Domain {
    public static class Res {
    private const string _default = "default";

    public const string ColorBrushWhite = "ColorBrushWhite";
    public const string ColorBrushDrive = "ColorBrushDrive";
    public const string ColorBrushFolder = "ColorBrushFolder";
    public const string ColorBrushPeople = "ColorBrushPeople";
    public const string ColorBrushTag = "ColorBrushTag";

    public const string IconEmpty = null;
    public const string IconCompare = "IconCompare";
    public const string IconFolder = "IconFolder";
    public const string IconFolderStar = "IconFolderStar";
    public const string IconFolderLock = "IconFolderLock";
    public const string IconFolderPuzzle = "IconFolderPuzzle";
    public const string IconFolderOpen = "IconFolderOpen";
    public const string IconTag = "IconTag";
    public const string IconTagLabel = "IconTagLabel";
    public const string IconPeople = "IconPeople";
    public const string IconPeopleMultiple = "IconPeopleMultiple";
    public const string IconDrive = "IconDrive";
    public const string IconDriveError = "IconDriveError";
    public const string IconCd = "IconCd";
    public const string IconSave = "IconSave";
    public const string IconSegment = "IconSegment";
    public const string IconSettings = "IconSettings";
    public const string IconStar = "IconStar";
    public const string IconSort = "IconSort";
    public const string IconRuler = "IconRuler";
    public const string IconLocationCheckin = "IconLocationCheckin";
    public const string IconEye = "IconEye";
    public const string IconMovieClapper = "IconMovieClapper";
    public const string IconInformation = "IconInformation";
    public const string IconNotification = "IconNotification";
    public const string IconQuestion = "IconQuestion";
    public const string IconBug = "IconBug";
    public const string IconImage = "IconImage";
    public const string IconImageMultiple = "IconImageMultiple";
    public const string IconCalendar = "IconCalendar";
    public const string IconEquals = "IconEquals";
    public const string IconCheckMark = "IconCheckMark";
    public const string IconXCross = "IconXCross";

    public static readonly Dictionary<object, object> IconToBrushDic = new() {
      { _default, ColorBrushWhite },
      { IconFolder, ColorBrushFolder },
      { IconFolderStar, ColorBrushFolder },
      { IconFolderLock, ColorBrushFolder },
      { IconFolderPuzzle, ColorBrushFolder },
      { IconFolderOpen, ColorBrushFolder },
      { IconTag, ColorBrushTag },
      { IconTagLabel, ColorBrushTag },
      { IconPeople, ColorBrushPeople },
      { IconPeopleMultiple, ColorBrushPeople },
      { IconDrive, ColorBrushDrive },
      { IconDriveError, ColorBrushDrive },
      { IconCd, ColorBrushDrive }
    };

    public static readonly Dictionary<object, object> TypeToTreeContextMenuDic = new() {
      { typeof(DriveM), "TreeContextMenuDrive" },
      { typeof(FolderM), "TreeContextMenuFolder" },
      { typeof(FavoriteFoldersTreeCategory), "TreeContextMenuFavoriteFolder" },
      { typeof(PeopleTreeCategory), "TreeContextMenuPeople" },
      { typeof(PersonM), "TreeContextMenuPerson" },
      { typeof(FolderKeywordsTreeCategory), "TreeContextMenuFolderKeywords" },
      { typeof(KeywordsTreeCategory), "TreeContextMenuKeywords" },
      { typeof(KeywordM), "TreeContextMenuKeyword" },
      { typeof(GeoNamesM), "TreeContextMenuGeoNames" },
      { typeof(GeoNameM), "TreeContextMenuGeoName" },
      { typeof(ViewersM), "TreeContextMenuViewers" },
      { typeof(ViewerM), "TreeContextMenuViewer" },
      { typeof(VideoClipsM), "TreeContextMenuVideoClips" },
      { typeof(CategoryGroupM), "TreeContextMenuGroup" }
    };

    public static string CategoryToIcon(Category category) =>
      category switch {
        Category.FavoriteFolders => IconFolderStar,
        Category.Folders => IconFolder,
        Category.Ratings => IconStar,
        Category.People => IconPeopleMultiple,
        Category.FolderKeywords => IconFolder,
        Category.Keywords => IconTagLabel,
        Category.Viewers => IconEye,
        Category.VideoClips => IconMovieClapper,
        _ => IconEmpty
      };

    public static string TypeToGroupByDialogTemplateKey(object o) =>
      o switch {
        PersonM => "PM.Views.List.PersonV",
        IListItem => "MH.DataTemplates.IListItem",
        _ => null
      };
  }
}
