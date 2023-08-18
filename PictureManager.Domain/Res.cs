using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using System;
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

    public static readonly Dictionary<Type, Tuple<string, string>> TypeToIconAndIconColorDic = new() {
      { typeof(CollectionViewPeople), new(IconPeopleMultiple, ColorBrushPeople) },
      { typeof(CollectionViewSegments), new(IconSegment, ColorBrushWhite) },
      { typeof(DriveM), new(IconDrive, ColorBrushDrive) },
      { typeof(FavoriteFolderM), new(IconFolder, ColorBrushFolder) },
      { typeof(FolderM), new(IconFolder, ColorBrushFolder) },
      { typeof(FolderKeywordsM), new(IconFolderPuzzle, ColorBrushFolder) },
      { typeof(GeoNameM), new(IconLocationCheckin, ColorBrushWhite) },
      { typeof(GeoNamesM), new(IconLocationCheckin, ColorBrushWhite) },
      { typeof(MediaItemsView), new(IconImageMultiple, ColorBrushWhite) },
      { typeof(PeopleM), new(IconPeopleMultiple, ColorBrushPeople) },
      { typeof(PeopleToolsTabM), new(IconPeopleMultiple, ColorBrushPeople) },
      { typeof(PeopleView), new(IconPeopleMultiple, ColorBrushPeople) },
      { typeof(PersonDetail), new(IconSegment, ColorBrushWhite) },
      { typeof(PersonM), new(IconPeople, ColorBrushPeople) },
      { typeof(SegmentsDrawerM), new(IconSegment, ColorBrushWhite) },
      { typeof(SegmentsView), new(IconSegment, ColorBrushWhite) },
      { typeof(KeywordM), new(IconTag, ColorBrushTag) },
      { typeof(KeywordsM), new(IconTagLabel, ColorBrushTag) },
      { typeof(VideoClipsTreeCategory), new(IconMovieClapper, ColorBrushWhite) },
      { typeof(ViewerM), new(IconEye, ColorBrushWhite) },
      { typeof(ViewersM), new(IconEye, ColorBrushWhite) }
    };

    public static readonly Dictionary<string, string> IconToIconColorDic = new() {
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

    public static string IconToIconColor(string icon) =>
      IconToIconColorDic.TryGetValue(icon, out var color)
        ? color
        : IconToIconColorDic[_default];

    public static string CategoryToIconName(Category category) =>
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
        IIconText => "PM.Views.List.IconTextV",
        _ => null
      };

    public static string TypeToIcon(object o, object parameter = null) =>
      TypeToIconAndIconColor(o, parameter).Item1;
    

    public static string TypeToIconColor(object o, object parameter = null) =>
      TypeToIconAndIconColor(o, parameter).Item2;

    public static Tuple<string, string> TypeToIconAndIconColor(object o, object parameter = null) {
      if (TypeToIconAndIconColorDic.TryGetValue(o.GetType(), out var icon))
        return icon;

      // TODO other types
      switch (o) {
        case CategoryGroupM g:
          var gIcon = CategoryToIconName(g.Category);
          return new(gIcon, IconToIconColor(gIcon));
        case CollectionViewGroup<MediaItemM> g:
          return TypeToIconAndIconColor(g.GroupedBy?.Data ?? g.View);
        case CollectionViewGroup<PersonM> g:
          return TypeToIconAndIconColor(g.GroupedBy?.Data ?? g.View);
        case CollectionViewGroup<SegmentM> g:
          return TypeToIconAndIconColor(g.GroupedBy?.Data ?? g.View);
        case IconText it:
          return new(it.IconName, IconToIconColor(it.IconName));
        default:
          return new(IconBug, ColorBrushWhite);
      }
    }
  }
}
