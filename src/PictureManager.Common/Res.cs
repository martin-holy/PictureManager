﻿using MH.Utils.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.TreeCategories;
using System.Collections.Generic;

namespace PictureManager.Common {
  public static class Res {
    private const string _default = "default";

    public const string ColorBrushWhite = "ColorBrushWhite";
    public const string ColorBrushDrive = "ColorBrushDrive";
    public const string ColorBrushFolder = "ColorBrushFolder";
    public const string ColorBrushPeople = "ColorBrushPeople";
    public const string ColorBrushTag = "ColorBrushTag";

    public const string IconBug = "IconBug";
    public const string IconCalendar = "IconCalendar";
    public const string IconCd = "IconCd";
    public const string IconCompare = "IconCompare";
    public const string IconDatabase = "IconDatabase";
    public const string IconDrawer = "IconDrawer";
    public const string IconDrawerAdd = "IconDrawerAdd";
    public const string IconDrive = "IconDrive";
    public const string IconDriveError = "IconDriveError";
    public const string IconEmpty = null;
    public const string IconEquals = "IconEquals";
    public const string IconEye = "IconEye";
    public const string IconFilter = "IconFilter";
    public const string IconFolder = "IconFolder";
    public const string IconFolderLock = "IconFolderLock";
    public const string IconFolderOpen = "IconFolderOpen";
    public const string IconFolderPuzzle = "IconFolderPuzzle";
    public const string IconFolderStar = "IconFolderStar";
    public const string IconImageMultiple = "IconImageMultiple";
    public const string IconInformation = "IconInformation";
    public const string IconLocationCheckin = "IconLocationCheckin";
    public const string IconMagnify = "IconMagnify";
    public const string IconNotification = "IconNotification";
    public const string IconPeople = "IconPeople";
    public const string IconPeopleMultiple = "IconPeopleMultiple";
    public const string IconPlus = "IconPlus";
    public const string IconRuler = "IconRuler";
    public const string IconSave = "IconSave";
    public const string IconSegment = "IconSegment";
    public const string IconSettings = "IconSettings";
    public const string IconSort = "IconSort";
    public const string IconStar = "IconStar";
    public const string IconTag = "IconTag";
    public const string IconTagLabel = "IconTagLabel";
    public const string IconUnknownSegment = "IconUnknownSegment";

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
      { typeof(FavoriteFolderM), "TreeContextMenuFavoriteFolder" },
      { typeof(PeopleTreeCategory), "TreeContextMenuPeople" },
      { typeof(PersonM), "TreeContextMenuPerson" },
      { typeof(FolderKeywordsTreeCategory), "TreeContextMenuFolderKeywords" },
      { typeof(KeywordsTreeCategory), "TreeContextMenuKeywords" },
      { typeof(KeywordM), "TreeContextMenuKeyword" },
      { typeof(GeoNamesTreeCategory), "TreeContextMenuGeoNames" },
      { typeof(GeoNameM), "TreeContextMenuGeoName" },
      { typeof(ViewersTreeCategory), "TreeContextMenuViewers" },
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

    public static string TypeToGroupByDialogTemplateKey(object o) =>
      o switch {
        PersonM => "PM.Views.List.PersonV",
        IListItem => "MH.DataTemplates.IListItem",
        _ => null
      };
  }
}
