﻿using MH.Utils;
using MH.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Common.Features.MediaItem;

public class MediaItemMetadata(RealMediaItemM mediaItem) {
  public RealMediaItemM MediaItem { get; } = mediaItem;
  public int Rating { get; set; }
  public string? Comment { get; set; }
  public int Width { get; set; }
  public int Height { get; set; }
  public Orientation Orientation { get; set; }
  public bool Success { get; set; }
  public string[]? Keywords { get; set; }
  public double? Lat { get; set; }
  public double? Lng { get; set; }
  public int? GeoNameId { get; set; }
  public List<Tuple<string, List<Tuple<string, string[]?>>>>? PeopleSegmentsKeywords { get; set; }

  public Task FindRefs() {
    MediaItem.Rating = Rating;
    MediaItem.Comment = Comment;
    MediaItem.Width = Width;
    MediaItem.Height = Height;
    MediaItem.Orientation = Orientation;
    MediaItem.SetThumbSize(true);
    FindPeople();
    FindKeywords();
    return FindGeoLocation();
  }

  public void FindPeople() {
    var oldSegments = MediaItem.Segments?.ToList();
    MediaItem.People = null;
    MediaItem.Segments = null;
    if (PeopleSegmentsKeywords == null) {
      Core.R.Segment.ItemsDelete(oldSegments);
      return;
    }

    MediaItem.People = new(PeopleSegmentsKeywords.Count);
    foreach (var psk in PeopleSegmentsKeywords) {
      var person = string.IsNullOrEmpty(psk.Item1) ? null : Core.R.Person.GetPerson(psk.Item1, true);

      // segments
      if (!psk.Item2.Any()) {
        if (person != null) MediaItem.People.Add(person);
        continue;
      }

      MediaItem.Segments ??= [];
        
      foreach (var c in psk.Item2) {
        var d = c.Item1.Split(",").Select(x => double.TryParse(x, CultureInfo.InvariantCulture, out var v) ? v : 0).ToArray();
        var x = (int)Math.Round(d[0] * MediaItem.Width);
        var y = (int)Math.Round(d[1] * MediaItem.Height);
        var s = (int)Math.Round(d[2] * MediaItem.Width);

        var segment = _recycleSegment(x, y, s, MediaItem, person, ref oldSegments);
        if (c.Item2 == null)
          segment.Keywords = null;
        else {
          segment.Keywords = [];
          foreach (var k in c.Item2) {
            var keyword = Core.R.Keyword.GetByFullPath(k);
            if (keyword != null)
              segment.Keywords.Add(keyword);
          }
        }
      }
    }

    Core.R.Segment.ItemsDelete(oldSegments?.Except(MediaItem.Segments.EmptyIfNull()).ToList());
  }

  private static SegmentM _recycleSegment(int x, int y, int s, MediaItemM mi, PersonM? person, ref List<SegmentM>? bin) {
    SegmentM segment;
    if (bin?.Any() == true) {
      segment = bin[^1];
      bin.RemoveAt(bin.Count - 1);
      segment.Size = 0; // not to trigger bound checks on X and Y
      segment.X = x;
      segment.Y = y;
      segment.Size = s;
      mi.Segments ??= [];
      mi.Segments.Add(segment);
      Core.R.Segment.IsModified = true;
    }
    else
      segment = Core.R.Segment.ItemCreate(x, y, s, mi);

    segment.Person = person;

    return segment;
  }

  public void FindKeywords() {
    MediaItem.Keywords = null;
    if (Keywords == null) return;
    MediaItem.Keywords = [];
    foreach (var k in Keywords.OrderByDescending(x => x).Distinct()) {
      var keyword = Core.R.Keyword.GetByFullPath(k.Replace('|', ' '));
      if (keyword != null)
        MediaItem.Keywords.Add(keyword);
    }
  }

  public async Task FindGeoLocation(bool online = true) =>
    Core.R.MediaItemGeoLocation.ItemUpdate(new(MediaItem,
      await Core.R.GeoLocation.GetOrCreate(Lat, Lng, GeoNameId, null, online)));
}