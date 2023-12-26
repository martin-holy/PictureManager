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

  public static IEnumerable<CollectionViewGroupByItem<T>> GetDatesFromMediaItems<T>(this IEnumerable<T> items) where T : MediaItemM {
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

  public static IEnumerable<CollectionViewGroupByItem<T>> GetFoldersFromMediaItems<T>(IEnumerable<T> items) where T : MediaItemM =>
    items
      .GetFolders()
      .ToGroupByItems<T, FolderM>(GroupMediaItemByFolder)
      .AsTree<CollectionViewGroupByItem<T>, FolderM, string>(x => x.FullPath);

  public static List<CollectionViewGroupByItem<SegmentM>> GetFoldersFromSegments(IList<SegmentM> segments) =>
    CollectionViewGroupByItem<SegmentM>.BuildTree<SegmentM, FolderM, string>(
      segments.GetFolders(),
      x => new(x, GroupSegmentByFolder),
      x => x.FullPath);

  public static List<CollectionViewGroupByItem<SegmentM>> GetKeywordsFromSegments(IEnumerable<SegmentM> segments) =>
    CollectionViewGroupByItem<SegmentM>.BuildTree<SegmentM, KeywordM, string>(
      segments.GetKeywords(),
      x => new(x, GroupSegmentByKeyword),
      x => x.FullName);

  public static List<CollectionViewGroupByItem<SegmentM>> GetPeopleFromSegments(IEnumerable<SegmentM> segments) =>
    segments
      .GetPeople()
      .OrderBy(x => x.Name)
      .Select(x => new CollectionViewGroupByItem<SegmentM>(x, GroupSegmentByPerson))
      .ToList();

  public static List<CollectionViewGroupByItem<PersonM>> GetKeywordsFromPeople(IEnumerable<PersonM> people) =>
    CollectionViewGroupByItem<PersonM>.BuildTree<PersonM, KeywordM, string>(
      people.GetKeywords(),
      x => new(x, GroupPersonByKeyword),
      x => x.FullName);

  public static CollectionViewGroupByItem<T> GetDatesInGroupFromMediaItems<T>(IEnumerable<T> items) where T : MediaItemM =>
    items
      .GetDatesFromMediaItems()
      .InGroup(_dateGroup, GroupMediaItemByDate);

  public static CollectionViewGroupByItem<T> GetKeywordsInGroupFromMediaItems<T>(IEnumerable<T> items) where T : MediaItemM =>
    items
      .GetKeywords()
      .ToGroupByItems<T, KeywordM>(GroupMediaItemByKeyword)
      .AsTree<CollectionViewGroupByItem<T>, KeywordM, string>(x => x.FullName)
      .InGroups<T, CategoryGroupM>(GroupMediaItemByKeyword)
      .InGroup(Core.KeywordsM.TreeCategory, GroupMediaItemByKeyword);

  public static CollectionViewGroupByItem<PersonM> GetKeywordsInGroupFromPeople(IEnumerable<PersonM> people) {
    var group = new CollectionViewGroupByItem<PersonM>(
      Core.KeywordsM.TreeCategory, GroupPersonByKeyword) { IsGroup = true };
    group.AddItems(GetItemsInGroups(GetKeywordsFromPeople(people), GroupPersonByKeyword));

    return group;
  }

  public static CollectionViewGroupByItem<SegmentM> GetKeywordsInGroupFromSegments(IEnumerable<SegmentM> segments) {
    var group = new CollectionViewGroupByItem<SegmentM>(
      Core.KeywordsM.TreeCategory, GroupSegmentByKeyword) { IsGroup = true };
    group.AddItems(GetItemsInGroups(GetKeywordsFromSegments(segments), GroupSegmentByKeyword));

    return group;
  }

  public static CollectionViewGroupByItem<T> GetPeopleInGroupFromMediaItems<T>(IEnumerable<T> items) where T : MediaItemM =>
    items
      .GetPeople()
      .OrderBy(x => x.Name)
      .Select(x => new CollectionViewGroupByItem<T>(x, GroupMediaItemByPerson))
      .InGroups<T, CategoryGroupM>(GroupMediaItemByPerson)
      .InGroup(Core.PeopleM.TreeCategory, GroupMediaItemByPerson);

  public static CollectionViewGroupByItem<SegmentM> GetPeopleInGroupFromSegments(IEnumerable<SegmentM> segments) {
    var group = new CollectionViewGroupByItem<SegmentM>(
      Core.PeopleM.TreeCategory, GroupSegmentByPerson) { IsGroup = true };
    group.AddItems(GetItemsInGroups(GetPeopleFromSegments(segments), GroupSegmentByPerson));

    return group;
  }

  public static List<CollectionViewGroupByItem<T>> GetItemsInGroups<T>(
    List<CollectionViewGroupByItem<T>> items, Func<T, object, bool> itemGroupBy) {

    var groups = items.GroupBy(x => (x.Data as ITreeItem)?.Parent as CategoryGroupM).ToArray();
    return groups
      .Where(x => x.Key != null)
      .OrderBy(x => x.Key.Name)
      .Select(x => {
        var group = new CollectionViewGroupByItem<T>(x.Key, itemGroupBy);
        group.Items.AddItems(x.Cast<ITreeItem>().ToArray(), item => item.Parent = group);

        return group;
      })
      .Concat(groups.Where(x => x.Key == null).SelectMany(x => x))
      .ToList();
  }

  public static CollectionViewGroupByItem<PersonM> GetPeopleGroupsInGroupFromPeople(IEnumerable<PersonM> people) {
    var group = new CollectionViewGroupByItem<PersonM>(
      _peopleGroupsGroup, GroupPersonByGroup) { IsGroup = true };
    var groupItems = people
      .GroupBy(x => x.Parent)
      .Select(x => x.Key)
      .OrderBy(x => x.Name)
      .Select(x => new CollectionViewGroupByItem<PersonM>(x, GroupPersonByGroup));

    group.AddItems(groupItems);

    return group;
  }

  public static bool GroupPersonByGroup(PersonM item, object parameter) =>
    ReferenceEquals(parameter, _peopleGroupsGroup)
    || ReferenceEquals(parameter, item.Parent);

  private static bool GroupMediaItemByDate(MediaItemM item, object parameter) =>
    ReferenceEquals(parameter, _dateGroup)
    || (item.FileName.Length > 7
        && parameter is DateM date
        && string.Equals(item.FileName[..8], date.Raw, StringComparison.Ordinal));

  private static bool GroupByFolder(FolderM folder, object parameter) =>
    parameter is FolderM f && folder.GetThisAndParents().Contains(f);

  private static bool GroupMediaItemByFolder(MediaItemM item, object parameter) =>
    GroupByFolder(item.Folder, parameter);

  private static bool GroupSegmentByFolder(SegmentM item, object parameter) =>
    GroupByFolder(item.MediaItem.Folder, parameter);

  private static bool GroupByPerson(PersonM person, object parameter) =>
    ReferenceEquals(parameter, person) || ReferenceEquals(parameter, person?.Parent);

  private static bool GroupMediaItemByPerson(MediaItemM item, object parameter) =>
    ReferenceEquals(parameter, Core.PeopleM.TreeCategory)
    || item.People?.Any(x => GroupByPerson(x, parameter)) == true
    || item.Segments?.Any(x => GroupByPerson(x.Person, parameter)) == true;

  private static bool GroupSegmentByPerson(SegmentM item, object parameter) =>
    ReferenceEquals(parameter, Core.PeopleM.TreeCategory)
    || GroupByPerson(item.Person, parameter);

  private static bool GroupByKeyword(IEnumerable<KeywordM> keywords, object parameter) =>
    ReferenceEquals(parameter, Core.KeywordsM.TreeCategory)
    || keywords?.SelectMany(x => x.GetThisAndParents<ITreeItem>()).Contains(parameter) == true;

  private static bool GroupMediaItemByKeyword(MediaItemM item, object parameter) =>
    GroupByKeyword(item.Keywords, parameter)
    || item.Segments?.Any(x => GroupByKeyword(x.Keywords, parameter)) == true;

  private static bool GroupPersonByKeyword(PersonM item, object parameter) =>
    GroupByKeyword(item.Keywords, parameter);

  private static bool GroupSegmentByKeyword(SegmentM item, object parameter) =>
    GroupByKeyword(item.Keywords, parameter);
}