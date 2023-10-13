using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Extensions;

public static class GetDataExtensions {
  public static IEnumerable<CategoryGroupM> GetCategoryGroups<T>(this T item) where T : ITreeItem =>
    item.Parent is CategoryGroupM cg
      ? cg.GetThisAndParents()
      : Enumerable.Empty<CategoryGroupM>();

  public static IEnumerable<FolderM> GetFolders(this MediaItemM mediaItem) =>
    mediaItem.Folder.GetThisAndParents();

  public static IEnumerable<FolderM> GetFolders(this IEnumerable<MediaItemM> mediaItems) =>
    mediaItems
      .EmptyIfNull()
      .SelectMany(x => x.GetFolders())
      .Distinct();

  public static IEnumerable<FolderM> GetFolders(this IEnumerable<SegmentM> segments) =>
    segments
      .GetMediaItems()
      .GetFolders();

  public static IEnumerable<FolderKeywordM> GetFolderKeywords(this FolderM folder) =>
    folder.FolderKeyword == null
      ? Enumerable.Empty<FolderKeywordM>()
      : folder.FolderKeyword.GetThisAndParents();

  public static IEnumerable<GeoNameM> GetGeoNames(this MediaItemM mediaItem) =>
    mediaItem.GeoName == null
      ? Enumerable.Empty<GeoNameM>()
      : mediaItem.GeoName.GetThisAndParents();

  public static IEnumerable<GeoNameM> GetGeoNames(this IEnumerable<MediaItemM> mediaItems) =>
    mediaItems
      .EmptyIfNull()
      .SelectMany(x => x.GetGeoNames())
      .Distinct();

  public static IEnumerable<KeywordM> GetKeywords(this MediaItemM mediaItem) =>
    mediaItem.Keywords
      .EmptyIfNull()
      .Concat(mediaItem.GetSegments().GetKeywords())
      .Distinct()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct();

  public static IEnumerable<KeywordM> GetKeywords(this MediaItemM[] mediaItems) =>
    mediaItems
      .EmptyIfNull()
      .SelectMany(x => x.GetKeywords())
      .Distinct();

  public static IEnumerable<KeywordM> GetKeywords(this IEnumerable<PersonM> people) =>
    people
      .EmptyIfNull()
      .Where(x => x.Keywords != null)
      .SelectMany(x => x.Keywords)
      .Distinct()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct();

  public static IEnumerable<KeywordM> GetKeywords(this IEnumerable<SegmentM> segments) =>
    segments
      .EmptyIfNull()
      .Where(x => x.Keywords != null)
      .SelectMany(x => x.Keywords)
      .Distinct()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct();

  public static IEnumerable<KeywordM> GetKeywords(this IEnumerable<KeywordM> keywords) =>
    keywords
      .EmptyIfNull()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct();

  public static IEnumerable<MediaItemM> GetMediaItems(this IEnumerable<SegmentM> segments) =>
    segments
      .EmptyIfNull()
      .Select(x => x.MediaItem)
      .Distinct();

  public static IEnumerable<PersonM> GetPeople(this MediaItemM mediaItem) =>
    mediaItem.People
      .EmptyIfNull()
      .Concat(mediaItem.Segments.GetPeople())
      .Distinct();

  public static IEnumerable<PersonM> GetPeople(this IEnumerable<MediaItemM> mediaItems) =>
    mediaItems
      .EmptyIfNull()
      .SelectMany(x => x.GetPeople())
      .Distinct();

  public static IEnumerable<PersonM> GetPeople(this IEnumerable<SegmentM> segments) =>
    segments
      .EmptyIfNull()
      .Where(x => x.Person != null)
      .Select(x => x.Person)
      .Distinct();

  public static IEnumerable<SegmentM> GetSegments(this MediaItemM mediaItem) =>
    mediaItem.Segments.EmptyIfNull();

  public static IEnumerable<SegmentM> GetSegments(this IEnumerable<MediaItemM> mediaItems) =>
    mediaItems
      .EmptyIfNull()
      .SelectMany(x => x.GetSegments());
}