using MH.UI.Controls;
using MH.UI.Extensions;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews;

public static class GroupByItems {
  private static readonly ListItem _dateGroup = new(Res.IconCalendar, "Date");
  private static readonly ListItem _peopleGroupsGroup = new(Res.IconPeopleMultiple, "Groups");

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

  public static IEnumerable<GroupByItem<T>> GetFolders<T>(IEnumerable<T> items)
    where T : MediaItemM =>
    items.GetFolders().ToGroupByItemsAsTree<T>(GroupByFolder);

  public static IEnumerable<GroupByItem<SegmentM>> GetFolders(IEnumerable<SegmentM> items) =>
    items.GetFolders().ToGroupByItemsAsTree<SegmentM>(GroupByFolder);

  public static IEnumerable<GroupByItem<T>> ToGroupByItemsAsTree<T>(this IEnumerable<FolderM> items, Func<T, object, bool> func)
    where T : class =>
    items.ToGroupByItems(func).AsTree<GroupByItem<T>, FolderM, string>(x => x.FullPath);

  public static IEnumerable<GroupByItem<SegmentM>> GetPeople(IEnumerable<SegmentM> items) =>
    items
      .GetPeople()
      .OrderBy(x => x.Name)
      .ToGroupByItems<SegmentM, PersonM>(GroupByPerson);

  public static GroupByItem<T> GetKeywordsInGroup<T>(IEnumerable<T> items) where T : MediaItemM =>
    items.GetKeywords().ToGroupedGroupByItemsInGroup<T>(GroupByKeyword);

  public static GroupByItem<PersonM> GetKeywordsInGroup(IEnumerable<PersonM> items) =>
    items.GetKeywords().ToGroupedGroupByItemsInGroup<PersonM>(GroupByKeyword);

  public static GroupByItem<SegmentM> GetKeywordsInGroup(IEnumerable<SegmentM> items) =>
    items.GetKeywords().ToGroupedGroupByItemsInGroup<SegmentM>(GroupByKeyword);

  private static GroupByItem<T> ToGroupedGroupByItemsInGroup<T>(this IEnumerable<KeywordM> items, Func<T, object, bool> func)
    where T : class =>
    items
      .ToGroupByItems(func)
      .AsTree<GroupByItem<T>, KeywordM, string>(x => x.FullName)
      .GroupByParent<T, CategoryGroupM>(func)
      .InGroup(Core.KeywordsM.TreeCategory, func);

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
      .InGroup(Core.PeopleM.TreeCategory, func);

  public static GroupByItem<PersonM> GetPeopleGroupsInGroup(IEnumerable<PersonM> people) =>
    people
      .GroupBy(x => x.Parent)
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

  private static bool GroupByFolder(MediaItemM item, object parameter) =>
    GroupByFolder(item.Folder, parameter);

  private static bool GroupByFolder(SegmentM item, object parameter) =>
    GroupByFolder(item.MediaItem.Folder, parameter);

  private static bool GroupByPerson(PersonM person, object parameter) =>
    ReferenceEquals(parameter, person) ||
    ReferenceEquals(parameter, person?.Parent);

  private static bool GroupByPerson(IEnumerable<PersonM> items, object parameter) =>
    ReferenceEquals(parameter, Core.PeopleM.TreeCategory) ||
    items?.Any(x => GroupByPerson(x, parameter)) == true;

  private static bool GroupByPerson(this MediaItemM item, object parameter) =>
    GroupByPerson(item.People, parameter) ||
    GroupByPerson(item.Segments?.GetPeople(), parameter);

  private static bool GroupByPerson(this SegmentM item, object parameter) =>
    ReferenceEquals(parameter, Core.PeopleM.TreeCategory) || 
    GroupByPerson(item.Person, parameter);

  private static bool GroupByKeyword(IEnumerable<KeywordM> items, object parameter) =>
    ReferenceEquals(parameter, Core.KeywordsM.TreeCategory) ||
    items?.SelectMany(x => x.GetThisAndParents<ITreeItem>()).Contains(parameter) == true;

  private static bool GroupByKeyword(this MediaItemM item, object parameter) =>
    GroupByKeyword(item.Keywords, parameter) ||
    GroupByKeyword(item.Segments?.GetKeywords(), parameter);

  private static bool GroupByKeyword(this PersonM item, object parameter) =>
    GroupByKeyword(item.Keywords, parameter);

  private static bool GroupByKeyword(this SegmentM item, object parameter) =>
    GroupByKeyword(item.Keywords, parameter);
}