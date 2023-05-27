using MH.Utils;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|MediaItemId|PersonId|SegmentBox|Keywords
  /// </summary>
  public class SegmentsDataAdapter : DataAdapter<SegmentM> {
    private readonly SegmentsM _model;
    private readonly MediaItemsM _mediaItemsM;
    private readonly PeopleM _peopleM;
    private readonly KeywordsM _keywordsM;

    public SegmentsDataAdapter(SegmentsM model, MediaItemsM mi, PeopleM p, KeywordsM k) : base("Segments", 5) {
      _model = model;
      _mediaItemsM = mi;
      _peopleM = p;
      _keywordsM = k;
    }

    public override void Save() =>
      SaveDriveRelated(All
        .GroupBy(x => Tree.GetTopParent(x.MediaItem.Folder))
        .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

    public override SegmentM FromCsv(string[] csv) {
      var rect = csv[3].Split(',');
      return new(int.Parse(csv[0]), int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2]));
    }

    public override string ToCsv(SegmentM segment) =>
      string.Join("|",
        segment.GetHashCode().ToString(),
        segment.MediaItem.GetHashCode().ToString(),
        segment.Person == null
          ? "0"
          : segment.Person.GetHashCode().ToString(),
        string.Join(",", ((int)segment.X).ToString(), ((int)segment.Y).ToString(), ((int)segment.Size).ToString()),
        segment.Keywords == null
          ? string.Empty
          : string.Join(",", segment.Keywords.Select(x => x.GetHashCode().ToString())));

    public override void PropsToCsv() {
      TableProps.Clear();
      TableProps.Add(nameof(_model.SegmentSize), _model.SegmentSize.ToString());
      TableProps.Add(nameof(_model.CompareSegmentSize), _model.CompareSegmentSize.ToString());
      TableProps.Add(nameof(_model.SimilarityLimit), _model.SimilarityLimit.ToString());
      TableProps.Add(nameof(_model.SimilarityLimitMin), _model.SimilarityLimitMin.ToString());
      TableProps.Add("SegmentsDrawer", string.Join(",", _model.SegmentsDrawerM.Items.Select(x => x.GetHashCode().ToString())));
    }

    public override void LinkReferences() {
      var withoutMediaItem = new List<SegmentM>();

      foreach (var (segment, csv) in AllCsv) {
        if (_mediaItemsM.DataAdapter.AllDict.TryGetValue(int.Parse(csv[1]), out var mi)) {
          segment.MediaItem = mi;
          mi.Segments ??= new();
          mi.Segments.Add(segment);

          var personId = int.Parse(csv[2]);

          if (personId != 0) {
            if (!_peopleM.DataAdapter.AllDict.TryGetValue(personId, out var person)) {
              // this needs to stay because not all segments have to be loaded
              // (segments from other drives)
              person = new(personId, $"P {personId}");
              _peopleM.DataAdapter.AllDict.Add(person.GetHashCode(), person);
            }

            segment.Person = person;
            person.Segment ??= segment;
          }
        }
        else {
          withoutMediaItem.Add(segment);
        }

        // reference to Keywords
        segment.Keywords = LinkList(csv[4], _keywordsM.DataAdapter.AllDict);
      }

      // in case MediaItem was deleted
      foreach (var segment in withoutMediaItem)
        _ = AllDict.Remove(segment.GetHashCode());

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
      if (TableProps.TryGetValue("SegmentsDrawer", out var segmentsDrawer) && !string.IsNullOrEmpty(segmentsDrawer)) {
        _model.SegmentsDrawerM.Items.Clear();

        var drawer = segmentsDrawer
          .Split(',')
          .Select(id => AllDict[int.Parse(id)])
          .OrderBy(x => x.MediaItem.Folder.FullPath)
          .ThenBy(x => x.MediaItem.FileName);

        foreach (var segment in drawer)
          _model.SegmentsDrawerM.Items.Add(segment);
      }

      // table props are not needed any more
      TableProps.Clear();
    }
  }
}
