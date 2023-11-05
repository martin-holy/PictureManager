using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PictureManager.Domain.HelperClasses;

public class MediaItemMetadata {
  public MediaItemM MediaItem { get; }
  public bool Success { get; set; }
  public string[] People { get; set; }
  public string[] Keywords { get; set; }
  public string GeoName { get; set; }
  public List<Tuple<string, List<Tuple<string, string[]>>>> PeopleSegmentsKeywords { get; set; }

  public MediaItemMetadata(MediaItemM mediaItem) {
    MediaItem = mediaItem;
  }

  public void FindRefs() {
    FindPeople();
    FindKeywords();
    FindGeoName();
  }

  public void FindPeople() {
    var oldSegments = MediaItem.Segments?.ToList();
    MediaItem.People = null;
    MediaItem.Segments = null;
    if (PeopleSegmentsKeywords == null) {
      Core.Db.Segments.ItemsDelete(oldSegments);
      return;
    }

    MediaItem.People = new(PeopleSegmentsKeywords.Count);
    foreach (var psk in PeopleSegmentsKeywords) {
      var person = Core.Db.People.GetPerson(psk.Item1, true);

      // segments
      if (!psk.Item2.Any()) {
        MediaItem.People.Add(person);
        continue;
      }

      MediaItem.Segments ??= new();
        
      foreach (var c in psk.Item2) {
        var d = c.Item1.Split(",").Select(x => double.TryParse(x, CultureInfo.InvariantCulture, out var v) ? v : 0).ToArray();
        var x = (int)Math.Round(d[0] * MediaItem.Width);
        var y = (int)Math.Round(d[1] * MediaItem.Height);
        var s = (int)Math.Round(d[2] * MediaItem.Width);

        var segment = RecycleSegment(x, y, s, MediaItem, person, ref oldSegments);
        if (c.Item2 == null)
          segment.Keywords = null;
        else {
          segment.Keywords = new();
          foreach (var k in c.Item2) {
            var keyword = Core.Db.Keywords.GetByFullPath(k);
            if (keyword != null)
              segment.Keywords.Add(keyword);
          }
        }
      }
    }

    Core.Db.Segments.ItemsDelete(oldSegments?.Except(MediaItem.Segments.EmptyIfNull()).ToList());
  }

  private static SegmentM RecycleSegment(int x, int y, int s, MediaItemM mi, PersonM person, ref List<SegmentM> bin) {
    SegmentM segment;
    if (bin?.Any() == true) {
      segment = bin[^1];
      bin.RemoveAt(bin.Count - 1);
      segment.X = x;
      segment.Y = y;
      segment.Size = s;
      mi.Segments.Add(segment);
      Core.Db.Segments.IsModified = true;
    }
    else
      segment = Core.Db.Segments.ItemCreate(x, y, s, mi);

    segment.Person = person;

    return segment;
  }

  public void FindKeywords() {
    MediaItem.Keywords = null;
    if (Keywords == null) return;
    MediaItem.Keywords = new();
    foreach (var k in Keywords.OrderByDescending(x => x).Distinct()) {
      var keyword = Core.Db.Keywords.GetByFullPath(k.Replace('|', ' '));
      if (keyword != null)
        MediaItem.Keywords.Add(keyword);
    }
  }

  public void FindGeoName() {
    if (!string.IsNullOrEmpty(GeoName)) {
      // TODO find/create GeoName
      MediaItem.GeoName = Core.Db.GeoNames.All
        .SingleOrDefault(x => x.GetHashCode() == int.Parse(GeoName));
    }
  }
}