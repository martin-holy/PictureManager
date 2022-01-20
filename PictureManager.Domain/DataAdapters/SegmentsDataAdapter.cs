using System;
using System.Collections.Generic;
using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|MediaItemId|PersonId|SegmentBox|Keywords
  /// </summary>
  public class SegmentsDataAdapter : DataAdapter {
    private readonly SegmentsM _model;
    private readonly MediaItemsM _mediaItemsM;
    private readonly PeopleM _peopleM;
    private readonly KeywordsM _keywordsM;

    public SegmentsDataAdapter(SimpleDB.SimpleDB db, SegmentsM model, MediaItemsM mi, PeopleM p, KeywordsM k)
      : base("Segments", db) {
      _model = model;
      _mediaItemsM = mi;
      _peopleM = p;
      _keywordsM = k;
    }

    public override void Load() {
      _model.All.Clear();
      _model.AllDic = new();
      LoadFromFile();
    }

    public override void Save() =>
      SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var rect = props[3].Split(',');
      var segment = new SegmentM(int.Parse(props[0]), int.Parse(props[2]), int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2])) {
        Csv = props
      };

      _model.All.Add(segment);
      _model.AllDic.Add(segment.Id, segment);
    }

    public static string ToCsv(SegmentM segment) =>
      string.Join("|",
        segment.Id.ToString(),
        segment.MediaItem.Id.ToString(),
        segment.PersonId.ToString(),
        string.Join(",", segment.X, segment.Y, segment.Radius),
        segment.Keywords == null
          ? string.Empty
          : string.Join(",", segment.Keywords.Select(x => x.Id)));

    public override void PropsToCsv() {
      TableProps.Clear();
      TableProps.Add(nameof(_model.SegmentSize), _model.SegmentSize.ToString());
      TableProps.Add(nameof(_model.CompareSegmentSize), _model.CompareSegmentSize.ToString());
      TableProps.Add(nameof(_model.SimilarityLimit), _model.SimilarityLimit.ToString());
      TableProps.Add(nameof(_model.SimilarityLimitMin), _model.SimilarityLimitMin.ToString());
      TableProps.Add(nameof(_model.SegmentsDrawer), string.Join(",", _model.SegmentsDrawer.Select(x => x.Id)));
    }

    public override void LinkReferences() {
      var withoutMediaItem = new List<SegmentM>();

      foreach (var segment in _model.All) {
        if (_mediaItemsM.AllDic.TryGetValue(int.Parse(segment.Csv[1]), out var mi)) {
          segment.MediaItem = mi;
          mi.Segments ??= new();
          mi.Segments.Add(segment);

          if (segment.PersonId > 0 && _peopleM.AllDic.TryGetValue(segment.PersonId, out var person)) {
            segment.Person = person;
            person.Segment ??= segment;
          }
        }
        else {
          withoutMediaItem.Add(segment);
        }

        // reference to Keywords
        if (!string.IsNullOrEmpty(segment.Csv[4])) {
          var ids = segment.Csv[4].Split(',');
          segment.Keywords = new(ids.Length);
          foreach (var keywordId in ids) {
            var k = _keywordsM.AllDic[int.Parse(keywordId)];
            segment.Keywords.Add(k);
          }
        }

        // CSV array is not needed any more
        segment.Csv = null;
      }

      // in case MediaItem was deleted
      foreach (var segment in withoutMediaItem)
        _ = _model.All.Remove(segment);

      // Table Properties
      if (TableProps == null) return;
      if (TableProps.TryGetValue(nameof(_model.SegmentSize), out var segmentSize))
        _model.SegmentSize = int.Parse(segmentSize);
      if (TableProps.TryGetValue(nameof(_model.CompareSegmentSize), out var compareSegmentSize))
        _model.CompareSegmentSize = int.Parse(compareSegmentSize);
      if (TableProps.TryGetValue(nameof(_model.SimilarityLimit), out var similarityLimit))
        _model.SimilarityLimit = int.Parse(similarityLimit);
      if (TableProps.TryGetValue(nameof(_model.SimilarityLimitMin), out var similarityLimitMin))
        _model.SimilarityLimitMin = int.Parse(similarityLimitMin);
      if (TableProps.TryGetValue(nameof(_model.SegmentsDrawer), out var segmentsDrawer) && !string.IsNullOrEmpty(segmentsDrawer)) {
        foreach (var segmentId in segmentsDrawer.Split(','))
          _model.SegmentsDrawer.Add(_model.AllDic[int.Parse(segmentId)]);
      }

      // table props are not needed any more
      TableProps.Clear();
    }
  }
}
