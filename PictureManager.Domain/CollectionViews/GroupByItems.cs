using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews;

public static class GroupByItems {
  private static readonly ListItem _dateGroup = new(Res.IconCalendar, "Date");
  private static readonly ListItem _peopleGroupsGroup = new(Res.IconPeopleMultiple, "Groups");
  private static readonly CategoryGroupM _unknownPeopleGroup =
    new(-1, "Unknown", Category.People, Res.IconPeopleMultiple);

  private static readonly Dictionary<string, string> _mediaItemsDates = new();
  private static readonly Dictionary<string, string> _dateFormats =
    new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

  public static List<CollectionViewGroupByItem<MediaItemM>> GetDatesFromMediaItems(IEnumerable<MediaItemM> mediaItems) {
    var list = new List<CollectionViewGroupByItem<MediaItemM>>();
    var dates = mediaItems
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
        list.Add(new(new DateM(Res.IconCalendar, value, key), GroupMediaItemByDate));
    }

    return list;
  }

  public static List<CollectionViewGroupByItem<MediaItemM>> GetFoldersFromMediaItems(IList<MediaItemM> mediaItems) =>
    CollectionViewGroupByItem<MediaItemM>.BuildTree<MediaItemM, FolderM, string>(
      mediaItems.Select(x => x.Folder),
      x => new(x, GroupMediaItemByFolder),
      x => x.FullPath);

  public static List<CollectionViewGroupByItem<SegmentM>> GetFoldersFromSegments(IList<SegmentM> segments) =>
    CollectionViewGroupByItem<SegmentM>.BuildTree<SegmentM, FolderM, string>(
      segments.Select(x => x.MediaItem.Folder),
      x => new(x, GroupSegmentByFolder),
      x => x.FullPath);

  public static List<CollectionViewGroupByItem<SegmentM>> GetKeywordsFromSegments(IEnumerable<SegmentM> segments) =>
    CollectionViewGroupByItem<SegmentM>.BuildTree<SegmentM, KeywordM, string>(
      segments.Where(x => x.Keywords != null).SelectMany(x => x.Keywords),
      x => new(x, GroupSegmentByKeyword),
      x => x.FullName);

  public static List<CollectionViewGroupByItem<MediaItemM>> GetPeopleFromMediaItems(IEnumerable<MediaItemM> mediaItems) =>
    mediaItems
      .SelectMany(mi => (mi.People ?? Enumerable.Empty<PersonM>())
        .Concat(
          mi.Segments == null
            ? Enumerable.Empty<PersonM>()
            : mi.Segments
              .Where(s => s.Person != null)
              .Select(s => s.Person)))
      .Distinct()
      .OrderBy(x => x.Name)
      .Select(x => new CollectionViewGroupByItem<MediaItemM>(x, GroupMediaItemByPerson))
      .ToList();

  public static List<CollectionViewGroupByItem<SegmentM>> GetPeopleFromSegments(IEnumerable<SegmentM> segments) =>
    segments
      .Where(x => x.Person != null)
      .Select(x => x.Person)
      .Distinct()
      .OrderBy(x => x.Name)
      .Select(x => new CollectionViewGroupByItem<SegmentM>(x, GroupSegmentByPerson))
      .ToList();

  public static List<CollectionViewGroupByItem<PersonM>> GetKeywordsFromPeople(IEnumerable<PersonM> people) =>
    CollectionViewGroupByItem<PersonM>.BuildTree<PersonM, KeywordM, string>(
      people.Where(x => x.Keywords != null).SelectMany(x => x.Keywords),
      x => new(x, GroupPersonByKeyword),
      x => x.FullName);

  public static CollectionViewGroupByItem<MediaItemM> GetDatesInGroupFromMediaItems(IEnumerable<MediaItemM> mediaItems) {
    var group = new CollectionViewGroupByItem<MediaItemM>(
      _dateGroup, GroupMediaItemByDate) { IsGroup = true };
    group.AddItems(GetDatesFromMediaItems(mediaItems));

    return group;
  }

  public static CollectionViewGroupByItem<PersonM> GetKeywordsInGroupFromPeople(IEnumerable<PersonM> people) {
    var group = new CollectionViewGroupByItem<PersonM>(
      Core.KeywordsM.TreeCategory, GroupPersonByKeyword) { IsGroup = true };
    group.AddItems(GetKeywordsFromPeople(people));

    return group;
  }

  public static CollectionViewGroupByItem<SegmentM> GetKeywordsInGroupFromSegments(IEnumerable<SegmentM> segments) {
    var group = new CollectionViewGroupByItem<SegmentM>(
      Core.KeywordsM.TreeCategory, GroupSegmentByKeyword) { IsGroup = true };
    group.AddItems(GetKeywordsFromSegments(segments));

    return group;
  }

  public static CollectionViewGroupByItem<MediaItemM> GetPeopleInGroupFromMediaItems(IEnumerable<MediaItemM> mediaItems) {
    var group = new CollectionViewGroupByItem<MediaItemM>(
      Core.PeopleM.TreeCategory, GroupMediaItemByPerson) { IsGroup = true };
    group.AddItems(GetPeopleFromMediaItems(mediaItems));

    return group;
  }

  public static CollectionViewGroupByItem<SegmentM> GetPeopleInGroupFromSegments(IEnumerable<SegmentM> segments) {
    var group = new CollectionViewGroupByItem<SegmentM>(
      Core.PeopleM.TreeCategory, GroupSegmentByPerson) { IsGroup = true };
    group.AddItems(GetPeopleFromSegments(segments));

    return group;
  }

  public static CollectionViewGroupByItem<PersonM> GetPeopleGroupsInGroupFromPeople(IEnumerable<PersonM> people) {
    var group = new CollectionViewGroupByItem<PersonM>(
      _peopleGroupsGroup, GroupPersonByGroup) { IsGroup = true };
    var groupItems = people
      .GroupBy(x => x.Parent)
      .Select(x => x.Key ?? _unknownPeopleGroup)
      .OrderBy(x => x.Name)
      .Select(x => new CollectionViewGroupByItem<PersonM>(x, GroupPersonByGroup));

    group.AddItems(groupItems);

    return group;
  }

  private static bool GroupMediaItemByDate(MediaItemM item, object parameter) =>
    ReferenceEquals(parameter, _dateGroup)
    || (item.FileName.Length > 7
        && parameter is DateM date
        && string.Equals(item.FileName[..8], date.Raw, StringComparison.Ordinal));

  private static bool GroupMediaItemByFolder(MediaItemM item, object parameter) =>
    parameter is FolderM folder
    && item.Folder
      .GetThisAndParents()
      .Contains(folder);

  private static bool GroupMediaItemByPerson(MediaItemM item, object parameter) =>
    ReferenceEquals(parameter, Core.PeopleM.TreeCategory)
    || item.People?.Any(x => ReferenceEquals(x, parameter)) == true
    || item.Segments?.Any(x => ReferenceEquals(x.Person, parameter)) == true;

  private static bool GroupSegmentByFolder(SegmentM item, object parameter) =>
    parameter is FolderM folder
    && item.MediaItem.Folder
      .GetThisAndParents()
      .Contains(folder);

  private static bool GroupSegmentByKeyword(SegmentM item, object parameter) =>
    ReferenceEquals(parameter, Core.KeywordsM.TreeCategory)
    || item.Keywords != null
    && parameter is KeywordM keyword
    && item.Keywords
      .SelectMany(x => x.GetThisAndParents())
      .Contains(keyword);

  private static bool GroupSegmentByPerson(SegmentM item, object parameter) =>
    ReferenceEquals(parameter, Core.PeopleM.TreeCategory)
    || ReferenceEquals(parameter, item.Person);

  public static bool GroupPersonByGroup(PersonM item, object parameter) =>
    ReferenceEquals(parameter, _peopleGroupsGroup)
    || ReferenceEquals(parameter, _unknownPeopleGroup) && item.Parent == null
    || ReferenceEquals(parameter, item.Parent);

  private static bool GroupPersonByKeyword(PersonM item, object parameter) =>
    ReferenceEquals(parameter, Core.KeywordsM.TreeCategory)
    || item.Keywords != null
    && parameter is KeywordM keyword
    && item.Keywords
      .SelectMany(x => x.GetThisAndParents())
      .Contains(keyword);
}