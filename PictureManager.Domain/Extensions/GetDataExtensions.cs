using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Extensions;

public static class GetDataExtensions {
  public static IEnumerable<FolderM> GetFolders(this IEnumerable<MediaItemM> mediaItems) =>
    mediaItems
      .EmptyIfNull()
      .SelectMany(x => x.Folder.GetThisAndParents())
      .Distinct();

  public static IEnumerable<FolderM> GetFolders(this IEnumerable<SegmentM> segments) =>
    segments
      .GetMediaItems()
      .GetFolders();

  public static IEnumerable<KeywordM> GetKeywords(this MediaItemM[] mediaItems) =>
    mediaItems
      .EmptyIfNull()
      .Where(x => x.Keywords != null)
      .SelectMany(x => x.Keywords)
      .Concat(mediaItems.GetSegments().GetKeywords())
      .Distinct()
      .SelectMany(x => x.GetThisAndParents())
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

  public static IEnumerable<PersonM> GetPeople(this IEnumerable<MediaItemM> mediaItems) =>
    mediaItems.EmptyIfNull()
      .SelectMany(mi => mi.People.EmptyIfNull()
        .Concat(mi.Segments.GetPeople()))
      .Distinct();

  public static IEnumerable<PersonM> GetPeople(this IEnumerable<SegmentM> segments) =>
    segments
      .EmptyIfNull()
      .Where(x => x.Person != null)
      .Select(x => x.Person)
      .Distinct();

  public static IEnumerable<SegmentM> GetSegments(this IEnumerable<MediaItemM> mediaItems) =>
    mediaItems
      .EmptyIfNull()
      .Where(x => x.Segments != null)
      .SelectMany(x => x.Segments);
}