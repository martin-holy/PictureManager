﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CollectionViews {
  public static class GroupByItems {
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
          list.Add(new(Res.IconCalendar, value, key, GroupMediaItemByDate));
      }

      return list;
    }

    public static List<CollectionViewGroupByItem<MediaItemM>> GetFoldersFromMediaItems(IList<MediaItemM> mediaItems) =>
      CollectionViewGroupByItem<MediaItemM>.BuildTree<MediaItemM, FolderM, string>(
        mediaItems.Select(x => x.Folder),
        x => new(Res.IconFolder, x.Name, x, GroupMediaItemByFolder),
        x => x.FullPath);

    public static List<CollectionViewGroupByItem<SegmentM>> GetFoldersFromSegments(IList<SegmentM> segments) =>
      CollectionViewGroupByItem<SegmentM>.BuildTree<SegmentM, FolderM, string>(
        segments.Select(x => x.MediaItem.Folder),
        x => new(Res.IconFolder, x.Name, x, GroupSegmentByFolder),
        x => x.FullPath);

    public static List<CollectionViewGroupByItem<SegmentM>> GetKeywordsFromSegments(IEnumerable<SegmentM> segments) =>
      CollectionViewGroupByItem<SegmentM>.BuildTree<SegmentM, KeywordM, string>(
        segments.Where(x => x.Keywords != null).SelectMany(x => x.Keywords),
        x => new(Res.IconTag, x.Name, x, GroupSegmentByKeyword),
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
        .Select(x => new CollectionViewGroupByItem<MediaItemM>(Res.IconPeople, x.Name, x, GroupMediaItemByPerson))
        .OrderBy(x => x.Name)
        .ToList();

    public static List<CollectionViewGroupByItem<SegmentM>> GetPeopleFromSegments(IEnumerable<SegmentM> segments) =>
      segments
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .Select(x => new CollectionViewGroupByItem<SegmentM>(Res.IconPeople, x.Name, x, GroupSegmentByPerson))
        .OrderBy(x => x.Name)
        .ToList();

    public static List<CollectionViewGroupByItem<PersonM>> GetKeywordsFromPeople(IEnumerable<PersonM> people) =>
      CollectionViewGroupByItem<PersonM>.BuildTree<PersonM, KeywordM, string>(
        people.Where(x => x.Keywords != null).SelectMany(x => x.Keywords),
        x => new(Res.IconTag, x.Name, x, GroupPersonByKeyword),
        x => x.FullName);

    public static CollectionViewGroupByItem<MediaItemM> GetDatesInGroupFromMediaItems(IEnumerable<MediaItemM> mediaItems) {
      var group = new CollectionViewGroupByItem<MediaItemM>(
        Res.IconCalendar, "Date", null, GroupMediaItemByDate) { IsGroup = true };
      group.AddItems(GetDatesFromMediaItems(mediaItems));

      return group;
    }

    public static CollectionViewGroupByItem<PersonM> GetKeywordsInGroupFromPeople(IEnumerable<PersonM> people) {
      var group = new CollectionViewGroupByItem<PersonM>(
        Res.IconTagLabel, "Keywords", Core.Instance.KeywordsM, GroupPersonByKeyword) { IsGroup = true };
      group.AddItems(GetKeywordsFromPeople(people));

      return group;
    }

    public static CollectionViewGroupByItem<SegmentM> GetKeywordsInGroupFromSegments(IEnumerable<SegmentM> segments) {
      var group = new CollectionViewGroupByItem<SegmentM>(
        Res.IconTagLabel, "Keywords", Core.Instance.KeywordsM, GroupSegmentByKeyword) { IsGroup = true };
      group.AddItems(GetKeywordsFromSegments(segments));

      return group;
    }

    public static CollectionViewGroupByItem<MediaItemM> GetPeopleInGroupFromMediaItems(IEnumerable<MediaItemM> mediaItems) {
      var group = new CollectionViewGroupByItem<MediaItemM>(
        Res.IconPeopleMultiple, "People", Core.Instance.PeopleM, GroupMediaItemByPerson) { IsGroup = true };
      group.AddItems(GetPeopleFromMediaItems(mediaItems));

      return group;
    }

    public static CollectionViewGroupByItem<SegmentM> GetPeopleInGroupFromSegments(IEnumerable<SegmentM> segments) {
      var group = new CollectionViewGroupByItem<SegmentM>(
        Res.IconPeopleMultiple, "People", Core.Instance.PeopleM, GroupSegmentByPerson) { IsGroup = true };
      group.AddItems(GetPeopleFromSegments(segments));

      return group;
    }

    public static CollectionViewGroupByItem<PersonM> GetPeopleGroupsInGroupFromPeople(IEnumerable<PersonM> people) {
      var group = new CollectionViewGroupByItem<PersonM>(
        Res.IconPeopleMultiple, "Groups", Core.Instance.CategoryGroupsM, GroupPersonByGroup) { IsGroup = true };
      var groupItems = people
        .GroupBy(x => x.Parent)
        .Select(x => x.Key ?? _unknownPeopleGroup)
        .Select(x => new CollectionViewGroupByItem<PersonM>(Res.IconPeopleMultiple, x.Name, x, GroupPersonByGroup))
        .OrderBy(x => x.Name);

      foreach (var groupItem in groupItems)
        group.AddItem(groupItem);

      return group;
    }

    private static bool GroupMediaItemByDate(MediaItemM item, object parameter) =>
      parameter == null
      || (item.FileName.Length > 7
          && string.Equals(item.FileName[..8], parameter as string, StringComparison.Ordinal));

    private static bool GroupMediaItemByFolder(MediaItemM item, object parameter) =>
      parameter is FolderM folder
      && item.Folder
        .GetThisAndParentRecursive()
        .Contains(folder);

    private static bool GroupMediaItemByPerson(MediaItemM item, object parameter) =>
      ReferenceEquals(parameter, Core.Instance.PeopleM)
      || item.People?.Any(x => ReferenceEquals(x, parameter)) == true
      || item.Segments?.Any(x => ReferenceEquals(x.Person, parameter)) == true;

    private static bool GroupSegmentByFolder(SegmentM item, object parameter) =>
      parameter is FolderM folder
      && item.MediaItem.Folder
        .GetThisAndParentRecursive()
        .Contains(folder);

    private static bool GroupSegmentByKeyword(SegmentM item, object parameter) =>
      ReferenceEquals(parameter, Core.Instance.KeywordsM)
      || item.Keywords != null
      && parameter is KeywordM keyword
      && item.Keywords
        .SelectMany(x => x.GetThisAndParentRecursive())
        .Contains(keyword);

    private static bool GroupSegmentByPerson(SegmentM item, object parameter) =>
      ReferenceEquals(parameter, Core.Instance.PeopleM)
      || ReferenceEquals(parameter, item.Person);

    public static bool GroupPersonByGroup(PersonM item, object parameter) =>
      ReferenceEquals(parameter, Core.Instance.CategoryGroupsM)
      || ReferenceEquals(parameter, _unknownPeopleGroup) && item.Parent == null
      || ReferenceEquals(parameter, item.Parent);

    private static bool GroupPersonByKeyword(PersonM item, object parameter) =>
      ReferenceEquals(parameter, Core.Instance.KeywordsM)
      || item.Keywords != null
      && parameter is KeywordM keyword
      && item.Keywords
        .SelectMany(x => x.GetThisAndParentRecursive())
        .Contains(keyword);
  }
}
