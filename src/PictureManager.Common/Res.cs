using MH.Utils.Interfaces;
using System.Collections.Generic;
using PictureManager.Common.Features.FavoriteFolder;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.FolderKeyword;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Viewer;

namespace PictureManager.Common;

public static class Res {
  private const string _default = "default";

  public const string BrushWhite = "PM.B.White";
  public const string BrushDrive = "PM.B.Drive";
  public const string BrushFolder = "PM.B.Folder";
  public const string BrushPeople = "PM.B.People";
  public const string BrushTag = "PM.B.Tag";

  public const string IconBug = "IconBug";
  public const string IconCalendar = "IconCalendar";
  public const string IconCd = "IconCd";
  public const string IconCompare = "IconCompare";
  public const string IconCopy = "IconCopy";
  public const string IconDatabase = "IconDatabase";
  public const string IconDrawer = "IconDrawer";
  public const string IconDrawerAdd = "IconDrawerAdd";
  public const string IconDrive = "IconDrive";
  public const string IconDriveError = "IconDriveError";
  public const string IconEmpty = "";
  public const string IconEquals = "IconEquals";
  public const string IconEye = "IconEye";
  public const string IconFilter = "IconFilter";
  public const string IconFolder = "IconFolder";
  public const string IconFolderLock = "IconFolderLock";
  public const string IconFolderOpen = "IconFolderOpen";
  public const string IconFolderPuzzle = "IconFolderPuzzle";
  public const string IconFolderStar = "IconFolderStar";
  public const string IconImageMultiple = "IconImageMultiple";
  public const string IconImport = "IconImport";
  public const string IconInformation = "IconInformation";
  public const string IconLocationCheckin = "IconLocationCheckin";
  public const string IconMagnify = "IconMagnify";
  public const string IconMove = "IconMove";
  public const string IconNotification = "IconNotification";
  public const string IconPeople = "IconPeople";
  public const string IconPeopleMultiple = "IconPeopleMultiple";
  public const string IconPlus = "IconPlus";
  public const string IconRotateClockwise = "IconRotateClockwise";
  public const string IconRotateLeft = "IconRotateLeft";
  public const string IconRotateRight = "IconRotateRight";
  public const string IconRuler = "IconRuler";
  public const string IconSave = "IconSave";
  public const string IconSegment = "IconSegment";
  public const string IconSettings = "IconSettings";
  public const string IconStar = "IconStar";
  public const string IconTabLeft = "IconTabLeft";
  public const string IconTabMiddle = "IconTabMiddle";
  public const string IconTabRight = "IconTabRight";
  public const string IconTag = "IconTag";
  public const string IconThreeBars = "IconThreeBars";
  public const string IconTagLabel = "IconTagLabel";
  public const string IconUnknownSegment = "IconUnknownSegment";
  
  public static readonly Dictionary<object, object> IconToBrushDic = new() {
    { _default, BrushWhite },
    { IconFolder, BrushFolder },
    { IconFolderStar, BrushFolder },
    { IconFolderLock, BrushFolder },
    { IconFolderPuzzle, BrushFolder },
    { IconFolderOpen, BrushFolder },
    { IconTag, BrushTag },
    { IconTagLabel, BrushTag },
    { IconPeople, BrushPeople },
    { IconPeopleMultiple, BrushPeople },
    { IconDrive, BrushDrive },
    { IconDriveError, BrushDrive },
    { IconCd, BrushDrive }
  };

  public static readonly Dictionary<object, object> TypeToTreeContextMenuDic = new() {
    { typeof(DriveM), "TreeContextMenuDrive" },
    { typeof(FolderM), "TreeContextMenuFolder" },
    { typeof(FavoriteFolderM), "TreeContextMenuFavoriteFolder" },
    { typeof(PersonTreeCategory), "TreeContextMenuPeople" },
    { typeof(PersonM), "TreeContextMenuPerson" },
    { typeof(FolderKeywordTreeCategory), "TreeContextMenuFolderKeywords" },
    { typeof(KeywordTreeCategory), "TreeContextMenuKeywords" },
    { typeof(KeywordM), "TreeContextMenuKeyword" },
    { typeof(GeoNameTreeCategory), "TreeContextMenuGeoNames" },
    { typeof(GeoNameM), "TreeContextMenuGeoName" },
    { typeof(ViewerTreeCategory), "TreeContextMenuViewers" },
    { typeof(ViewerM), "TreeContextMenuViewer" },
    { typeof(KeywordCategoryGroupM), "TreeContextMenuKeywordGroup" },
    { typeof(PersonCategoryGroupM), "TreeContextMenuPersonGroup" }
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
      Category.VideoClips => MH.UI.Res.IconMovieClapper,
      _ => IconEmpty
    };

  public static string? TypeToGroupByDialogTemplateKey(object o) =>
    o switch {
      PersonM => "PM.DT.Person.ListItem",
      IListItem => "MH.DT.IListItem",
      _ => null
    };
}