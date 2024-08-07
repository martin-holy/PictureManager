﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Common;

public static class GroupByItems {
  private static readonly ListItem _dateGroup = new(Res.IconCalendar, "Date");
  private static readonly ListItem _peopleGroupsGroup = new(Res.IconPeopleMultiple, "Groups");
  private static readonly ListItem _segmentSizeGroup = new(Res.IconRuler, "Size");

  private static readonly Dictionary<string, string> _mediaItemsDates = new();
  private static readonly Dictionary<string, string> _dateFormats =
    new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

  public static GroupByItem<T> GetDatesInGroup<T>(IEnumerable<T> items) where T : MediaItemM =>
    items
      .GetDates()
      .InGroup(_dateGroup, GroupMediaItemByDate);

  public static IEnumerable<GroupByItem<T>> GetDates<T>(this IEnumerable<T> items) where T : MediaItemM {
    var dates = items
      .Where(x => x.FileName.Length > 8)
      .Select(x => x.FileName[..8])
      .Distinct();

    foreach (var key in dates) {
      if (!_mediaItemsDates.TryGetValue(key, out var value)) {
        value = DateTimeExtensions.DateFromString(key, _dateFormats);
        // TODO check what is stored, how much is it
        _mediaItemsDates.Add(key, value);
      }

      if (!string.IsNullOrEmpty(value))
        yield return new(new DateM(Res.IconCalendar, value, key), GroupMediaItemByDate);
    }
  }

  public static GroupByItem<SegmentM> GetSegmentSizesInGroup(IEnumerable<SegmentM> items) =>
    items
      .GetSegmentSizes()
      .InGroup(_segmentSizeGroup, GroupSegmentBySize);

  public static IEnumerable<GroupByItem<SegmentM>> GetSegmentSizes(this IEnumerable<SegmentM> items) =>
    items
      .GroupBy(x => (int)(x.Size / Core.Settings.Segment.GroupSize))
      .Distinct()
      .OrderBy(x => x.Key)
      .Select(x => {
        var min = x.Key * Core.Settings.Segment.GroupSize + 1;
        var max = min + Core.Settings.Segment.GroupSize - 1;
        var text = max.ToString();
        return new GroupByItem<SegmentM>(new SizeM(Res.IconRuler, text, min, max), GroupSegmentBySize);
      });

  public static IEnumerable<GroupByItem<T>> GetFolders<T>(IEnumerable<T> items)
    where T : MediaItemM =>
    items.GetFolders().ToGroupByItemsAsTree<T>(GroupByFolder);

  public static IEnumerable<GroupByItem<PersonM>> GetFolders(IEnumerable<PersonM> items) =>
    items.GetFolders().ToGroupByItemsAsTree<PersonM>(GroupByFolder);

  public static IEnumerable<GroupByItem<SegmentM>> GetFolders(IEnumerable<SegmentM> items) =>
    items.GetFolders().ToGroupByItemsAsTree<SegmentM>(GroupByFolder);

  public static IEnumerable<GroupByItem<T>> ToGroupByItemsAsTree<T>(this IEnumerable<FolderM> items, Func<T, object, bool> func)
    where T : class =>
    items.ToGroupByItems(func).AsTree<GroupByItem<T>, FolderM, string>(x => x.FullPath);

  public static IEnumerable<GroupByItem<T>> GetGeoNames<T>(IEnumerable<T> items)
    where T : MediaItemM =>
    items.GetGeoNames().ToGroupByItemsAsTree<T>(GroupByGeoName);

  public static IEnumerable<GroupByItem<SegmentM>> GetGeoNames(IEnumerable<SegmentM> items) =>
    items.GetGeoNames().ToGroupByItemsAsTree<SegmentM>(GroupByGeoName);

  public static IEnumerable<GroupByItem<T>> ToGroupByItemsAsTree<T>(this IEnumerable<GeoNameM> items, Func<T, object, bool> func)
    where T : class =>
    items.ToGroupByItems(func).AsTree<GroupByItem<T>, GeoNameM, string>(x => x.FullName);

  public static IEnumerable<GroupByItem<SegmentM>> GetPeople(IEnumerable<SegmentM> items) =>
    items
      .GetPeople()
      .OrderBy(x => x.Name)
      .ToGroupByItems<SegmentM, PersonM>(GroupByPerson);

  public static GroupByItem<T> GetKeywordsInGroup<T>(IEnumerable<T> items) where T : class, IHaveKeywords =>
    items.GetKeywords().ToGroupedGroupByItemsInGroup<T>(GroupByKeyword);

  private static GroupByItem<T> ToGroupedGroupByItemsInGroup<T>(this IEnumerable<KeywordM> items, Func<T, object, bool> func)
    where T : class =>
    items
      .ToGroupByItems(func)
      .AsTree<GroupByItem<T>, KeywordM, string>(x => x.FullName)
      .GroupByParent<T, CategoryGroupM>(func)
      .InGroup(Core.R.Keyword.Tree, func);

  public static GroupByItem<T> GetPeopleInGroup<T>(IEnumerable<T> items) where T : MediaItemM =>
    items.GetPeople().ToGroupedGroupByItemsInGroup<T>(GroupByPerson);

  public static GroupByItem<SegmentM> GetPeopleInGroup(IEnumerable<SegmentM> items) =>
    items.GetPeople().ToGroupedGroupByItemsInGroup<SegmentM>(GroupByPerson);

  private static GroupByItem<T> ToGroupedGroupByItemsInGroup<T>(this IEnumerable<PersonM> items, Func<T, object, bool> func)
    where T : class =>
    items
      .OrderBy(x => x.Name)
      .ToGroupByItems(func)
      .GroupByParent<T, CategoryGroupM>(func)
      .InGroup(Core.R.Person.Tree, func);

  public static GroupByItem<PersonM> GetPeopleGroupsInGroup(IEnumerable<PersonM> people) =>
    people
      .GroupBy(x => x.Parent!)
      .Select(x => x.Key)
      .OrderBy(x => x.Name)
      .ToGroupByItems<PersonM, ITreeItem>(GroupPersonByGroup)
      .InGroup(_peopleGroupsGroup, GroupPersonByGroup);

  public static bool GroupPersonByGroup(PersonM item, object parameter) =>
    ReferenceEquals(parameter, _peopleGroupsGroup) ||
    ReferenceEquals(parameter, item.Parent);

  private static bool GroupMediaItemByDate(MediaItemM item, object parameter) =>
    ReferenceEquals(parameter, _dateGroup) ||
    (item.FileName.Length > 7 && 
     parameter is DateM date && 
     string.Equals(item.FileName[..8], date.Raw, StringComparison.Ordinal));

  private static bool GroupByFolder(FolderM folder, object parameter) =>
    parameter is FolderM f && folder.GetThisAndParents().Contains(f);

  private static bool GroupByFolder(IEnumerable<FolderM> items, object parameter) =>
    items.Any(x => GroupByFolder(x, parameter));

  private static bool GroupByFolder(MediaItemM item, object parameter) =>
    GroupByFolder(item.Folder, parameter);

  private static bool GroupByFolder(PersonM item, object parameter) =>
    GroupByFolder(item.GetFolders(), parameter);

  private static bool GroupByFolder(SegmentM item, object parameter) =>
    GroupByFolder(item.MediaItem.Folder, parameter);

  private static bool GroupByGeoName(GeoNameM? geoName, object parameter) =>
    geoName?.GetThisAndParents().Contains(parameter as GeoNameM) == true;

  private static bool GroupByGeoName(MediaItemM item, object parameter) =>
    GroupByGeoName(item.GeoLocation?.GeoName, parameter);

  private static bool GroupByGeoName(SegmentM item, object parameter) =>
    GroupByGeoName(item.MediaItem.GeoLocation?.GeoName, parameter);

  private static bool GroupByPerson(PersonM? person, object parameter) =>
    ReferenceEquals(parameter, person) ||
    ReferenceEquals(parameter, person?.Parent);

  private static bool GroupByPerson(IEnumerable<PersonM> items, object parameter) =>
    ReferenceEquals(parameter, Core.R.Person.Tree) ||
    items.Any(x => GroupByPerson(x, parameter));

  private static bool GroupByPerson(this MediaItemM item, object parameter) =>
    GroupByPerson(item.GetPeople(), parameter);

  private static bool GroupByPerson(this SegmentM item, object parameter) =>
    ReferenceEquals(parameter, Core.R.Person.Tree) || 
    GroupByPerson(item.Person, parameter);

  private static bool GroupSegmentBySize(SegmentM item, object parameter) =>
    ReferenceEquals(parameter, _segmentSizeGroup) ||
    (parameter is SizeM size && size.Fits((int)item.Size));

  private static bool GroupByKeyword(IEnumerable<KeywordM> items, object parameter) =>
    ReferenceEquals(parameter, Core.R.Keyword.Tree) ||
    items.SelectMany(x => x.GetThisAndParents<ITreeItem>()).Contains(parameter);

  private static bool GroupByKeyword(this IHaveKeywords item, object parameter) =>
    GroupByKeyword(item.GetKeywords(), parameter);
}