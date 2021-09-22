using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models {
  public sealed class Segments : ObservableObject, ITable {
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, Segment> AllDic { get; set; }

    private int _segmentSize = 100;
    private int _compareSegmentSize = 32;
    private int _similarityLimit = 90;
    private int _similarityLimitMin = 80;
    private bool _groupSegments;
    private bool _groupConfirmedSegments;
    private List<Segment> _selected = new();

    public List<Segment> Loaded { get; } = new();
    public List<List<Segment>> LoadedGroupedByPerson { get; } = new();
    public List<Segment> Selected => _selected;
    public List<Segment> SegmentsDrawer { get; } = new();
    public List<(int personId, Segment segment, List<(int personId, Segment segment, double sim)> similar)> ConfirmedSegments { get; } = new();
    public int SegmentSize { get => _segmentSize; set { _segmentSize = value; OnPropertyChanged(); } }
    public int CompareSegmentSize { get => _compareSegmentSize; set { _compareSegmentSize = value; OnPropertyChanged(); } }
    public int SimilarityLimit { get => _similarityLimit; set { _similarityLimit = value; OnPropertyChanged(); } }
    public int SimilarityLimitMin { get => _similarityLimitMin; set { _similarityLimitMin = value; OnPropertyChanged(); } }
    public int SelectedCount => Selected.Count;
    public bool GroupSegments { get => _groupSegments; set { _groupSegments = value; OnPropertyChanged(); } }
    public bool GroupConfirmedSegments { get => _groupConfirmedSegments; set { _groupConfirmedSegments = value; OnPropertyChanged(); } }

    #region ITable Methods
    public void LoadFromFile() {
      All.Clear();
      AllDic = new Dictionary<int, Segment>();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|MediaItemId|PersonId|SegmentBox|Keywords
      var props = csv.Split('|');
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var rect = props[3].Split(',');
      var segment = new Segment(int.Parse(props[0]), int.Parse(props[2]), int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2])) {
        Csv = props
      };

      All.Add(segment);
      AllDic.Add(segment.Id, segment);
    }

    public void LinkReferences() {
      var mediaItems = Core.Instance.MediaItems.AllDic;
      var people = Core.Instance.People.AllDic;
      var withoutMediaItem = new List<Segment>();

      foreach (var segment in All.Cast<Segment>()) {
        if (mediaItems.TryGetValue(int.Parse(segment.Csv[1]), out var mi)) {
          segment.MediaItem = mi;
          mi.Segments ??= new();
          mi.Segments.Add(segment);

          if (segment.PersonId > 0 && people.TryGetValue(segment.PersonId, out var person)) {
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
            var k = Core.Instance.Keywords.AllDic[int.Parse(keywordId)];
            segment.Keywords.Add(k);
          }
        }

        // CSV array is not needed any more
        segment.Csv = null;
      }

      // in case MediaItem was deleted
      foreach (var segment in withoutMediaItem)
        _ = All.Remove(segment);

      // Table Properties
      if (Helper.TableProps == null) return;
      if (Helper.TableProps.TryGetValue(nameof(SegmentSize), out var segmentSize))
        SegmentSize = int.Parse(segmentSize);
      if (Helper.TableProps.TryGetValue(nameof(CompareSegmentSize), out var fompareSegmentSize))
        CompareSegmentSize = int.Parse(fompareSegmentSize);
      if (Helper.TableProps.TryGetValue(nameof(SimilarityLimit), out var similarityLimit))
        SimilarityLimit = int.Parse(similarityLimit);
      if (Helper.TableProps.TryGetValue(nameof(SimilarityLimitMin), out var similarityLimitMin))
        SimilarityLimitMin = int.Parse(similarityLimitMin);
      if (Helper.TableProps.TryGetValue(nameof(SegmentsDrawer), out var segmentsDrawer) && !string.IsNullOrEmpty(segmentsDrawer)) {
        foreach (var segmentId in segmentsDrawer.Split(','))
          SegmentsDrawer.Add(AllDic[int.Parse(segmentId)]);
      }

      // table props are not needed any more
      Helper.TableProps.Clear();
      Helper.TableProps = null;
    }

    public void TablePropsToCsv() {
      Helper.TableProps = new();
      Helper.TableProps.Add(nameof(SegmentSize), SegmentSize.ToString());
      Helper.TableProps.Add(nameof(CompareSegmentSize), CompareSegmentSize.ToString());
      Helper.TableProps.Add(nameof(SimilarityLimit), SimilarityLimit.ToString());
      Helper.TableProps.Add(nameof(SimilarityLimitMin), SimilarityLimitMin.ToString());
      Helper.TableProps.Add(nameof(SegmentsDrawer), string.Join(",", SegmentsDrawer.Select(x => x.Id)));
    }
    #endregion

    public void Select(List<Segment> list, Segment segment, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select<Segment>(ref _selected, list, (ISelectable)segment, isCtrlOn, isShiftOn, () => OnPropertyChanged(nameof(SelectedCount)));

    public void DeselectAll() => Selecting.DeselectAll<Segment>(ref _selected, () => OnPropertyChanged(nameof(SelectedCount)));

    public void SetSelected(Segment segment, bool value) => Selecting.SetSelected<Segment>(ref _selected, segment, value, () => OnPropertyChanged(nameof(SelectedCount)));

    public bool SegmentsDrawerToggle(Segment segment) {
      if (segment == null) return false;
      Extensions.Toggle(SegmentsDrawer, segment, false);
      Helper.AreTablePropsModified = true;
      Core.Instance.Sdb.Changes++;
      return true;
    }

    private void ResetBeforeNewLoad() {
      DeselectAll();
      Loaded.Clear();
      LoadedGroupedByPerson.ForEach(x => x.Clear());
      LoadedGroupedByPerson.Clear();
    }

    public Segment[] GetOneOrSelected(Segment one) => Selected.Contains(one) ? Selected.ToArray() : new Segment[] { one };

    public Segment[] GetSegments(List<MediaItem> mediaItems, bool withPersonOnly) {
      if (withPersonOnly) {
        var people = mediaItems.Where(mi => mi.Segments != null).SelectMany(mi => mi.Segments.Select(s => s.PersonId)).Distinct().ToHashSet();
        people.Remove(0);
        return All.Cast<Segment>().Where(s => people.Contains(s.PersonId)).OrderBy(s => s.MediaItem.FileName).ToArray();
      }
      else {
        return mediaItems.Where(x => x.Segments != null).SelectMany(x => x.Segments).ToArray();
      }
    }

    public async IAsyncEnumerable<Segment> LoadSegmentsAsync(Segment[] segments, IProgress<int> progress, [EnumeratorCancellation] CancellationToken token = default) {
      ResetBeforeNewLoad();
      var done = 0;

      foreach (var segment in segments) {
        if (token.IsCancellationRequested) yield break;

        await segment.SetPictureAsync(SegmentSize);
        segment.MediaItem.SetThumbSize();
        Loaded.Add(segment);
        progress.Report(++done);

        yield return segment;
      }
    }

    public Segment AddNewSegment(int x, int y, int radius, MediaItem mediaItem) {
      var newSegment = new Segment(Helper.GetNextId(), 0, x, y, radius) { MediaItem = mediaItem };
      _ = newSegment.SetPictureAsync(SegmentSize);
      mediaItem.Segments ??= new();
      mediaItem.Segments.Add(newSegment);
      All.Add(newSegment);
      Loaded.Add(newSegment);

      return newSegment;
    }

    public Segment GetCopy(Segment s) =>
      new Segment(Helper.GetNextId(), s.PersonId, s.X, s.Y, s.Radius) {
        MediaItem = s.MediaItem,
        Person = s.Person,
        Keywords = s.Keywords?.ToList()
      };

    public async Task AddSegmentsForComparison() {
      var people = Loaded.Select(s => s.PersonId).Distinct().ToHashSet();
      people.Remove(0);
      var newSegments = All.Cast<Segment>().Where(s => people.Contains(s.PersonId)).Except(Loaded);

      foreach (var segment in newSegments) {
        await segment.SetPictureAsync(SegmentSize);
        segment.MediaItem.SetThumbSize();
        Loaded.Add(segment);
      }
    }

    public Task FindSimilaritiesAsync(IEnumerable<Segment> segments, IProgress<int> progress, CancellationToken token) {
      return Task.Run(async () => {
        // clear previous loaded similar
        // clear needs to be for Loaded!
        foreach (var segment in Loaded) {
          segment.Similar?.Clear();
          segment.SimMax = 0;
        }

        var tm = new Accord.Imaging.ExhaustiveTemplateMatching(0);
        var done = 0;

        foreach (var segmentA in segments) {
          if (token.IsCancellationRequested) break;
          await segmentA.SetPictureAsync(SegmentSize);
          await segmentA.SetComparePictureAsync(CompareSegmentSize);
          if (segmentA.ComparePicture == null) {
            progress.Report(++done);
            Core.Instance.LogError(new Exception($"Picture with unsupported pixel format.\n{segmentA.MediaItem.FilePath}"));
            continue;
          }

          segmentA.Similar ??= new();
          foreach (var segmentB in segments) {
            if (token.IsCancellationRequested) break;
            // do not compare segment with it self or with segment that have same person
            if (segmentA == segmentB || (segmentA.PersonId != 0 && segmentA.PersonId == segmentB.PersonId)) continue;
            // do not compare segment with PersonId > 0 with segment with also PersonId > 0
            if (segmentA.PersonId > 0 && segmentB.PersonId > 0) continue;

            await segmentA.SetPictureAsync(SegmentSize);
            await segmentB.SetComparePictureAsync(CompareSegmentSize);
            if (segmentB.ComparePicture == null) continue;

            var matchings = tm.ProcessImage(segmentB.ComparePicture, segmentA.ComparePicture);
            var sim = Math.Round(matchings.Max(x => x.Similarity) * 100, 1);
            if (sim < SimilarityLimitMin) continue;

            segmentA.Similar.Add(segmentB, sim);
            if (segmentA.SimMax < sim) segmentA.SimMax = sim;
          }

          if (segmentA.Similar.Count == 0) {
            segmentA.Similar = null;
            segmentA.SimMax = 0;
          }

          progress.Report(++done);
        }
      }, token);
    }

    /// <summary>
    /// Compares segments with same person id to other segments with same person id
    /// and select random segment from each group for display
    /// </summary>
    public async Task ReloadConfirmedSegments() {
      var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Title);
      var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
      var groups = groupsA.Concat(groupsB);
      var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

      async Task<Segment> GetRandomSegment(IEnumerable<Segment> segments) {
        var _segments = segments.First().Person?.Segments ?? segments;
        var segment = _segments.ToArray()[random.Next(_segments.Count() - 1)];
        await segment.SetPictureAsync(SegmentSize);
        segment.MediaItem.SetThumbSize();
        return segment;
      }

      ConfirmedSegments.Clear();

      foreach (var gA in groups) {
        (int personId, Segment segment, List<(int personId, Segment segment, double sim)> similar) confirmedSegment = new() {
          personId = gA.Key,
          segment = await GetRandomSegment(gA),
          similar = new()
        };

        ConfirmedSegments.Add(confirmedSegment);
        if (!gA.Any(x => x.Similar != null)) continue;

        foreach (var gB in groups) {
          if (gA.Key == gB.Key) continue;

          var sims = new List<double>();

          foreach (var segmentA in gA)
            foreach (var segmentB in gB)
              if (segmentA.Similar != null && segmentA.Similar.TryGetValue(segmentB, out var sim) && sim >= SimilarityLimit)
                sims.Add(sim);

          if (sims.Count == 0) continue;

          var simMedian = sims.OrderBy(x => x).ToArray()[sims.Count / 2];
          confirmedSegment.similar.Add(new(gB.Key, await GetRandomSegment(gB), simMedian));
        }
      }
    }

    public Task ReloadLoadedGroupedByPersonAsync() {
      return Task.Run(() => {
        // clear
        foreach (var group in LoadedGroupedByPerson)
          group.Clear();
        LoadedGroupedByPerson.Clear();

        List<Segment> segmentsGroup;

        // add segments with PersonId != 0 with all similar segments with PersonId == 0
        var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Title);
        var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
        var samePerson = groupsA.Concat(groupsB);
        foreach (var segments in samePerson) {
          var sims = new List<(Segment segment, double sim)>();
          segmentsGroup = new();

          foreach (var segment in segments.OrderBy(x => x.MediaItem.FileName)) {
            segmentsGroup.Add(segment);
            if (segment.Similar == null) continue;
            sims.AddRange(segment.Similar.Where(x => x.Key.PersonId == 0 && x.Value >= SimilarityLimit).Select(x => (x.Key, x.Value)));
          }

          // order by number of similar than by similarity
          foreach (var segment in sims.GroupBy(x => x.segment).OrderByDescending(g => g.Count())
                                   .Select(g => g.OrderByDescending(x => x.sim).First().segment)) {
            segmentsGroup.Add(segment);
          }

          if (segmentsGroup.Count != 0)
            LoadedGroupedByPerson.Add(segmentsGroup);
        }

        // add segments with PersonId == 0 ordered by similar
        var unknown = Loaded.Where(x => x.PersonId == 0);
        var withSimilar = unknown.Where(x => x.Similar != null).OrderByDescending(x => x.SimMax);
        var set = new HashSet<int>();
        segmentsGroup = new();

        foreach (var segment in withSimilar) {
          var simSegments = segment.Similar.Where(x => x.Key.PersonId == 0 && x.Value >= SimilarityLimit).OrderByDescending(x => x.Value);
          foreach (var simSegment in simSegments) {
            if (set.Add(segment.Id)) segmentsGroup.Add(segment);
            if (set.Add(simSegment.Key.Id)) segmentsGroup.Add(simSegment.Key);
          }
        }

        // add rest of the segments
        foreach (var segment in unknown.Where(x => !set.Contains(x.Id)))
          segmentsGroup.Add(segment);

        if (segmentsGroup.Count != 0)
          LoadedGroupedByPerson.Add(segmentsGroup);
      });
    }

    /// <summary>
    /// Sets new Person to all Segments that are selected or that have the same PersonId (< 0) as some of the selected.
    /// </summary>
    /// <param name="person"></param>
    public void SetSelectedAsPerson(Person person) {
      var unknownPeople = Selected.Select(x => x.PersonId).Distinct().Where(x => x < 0).ToDictionary(x => x);
      var segments = Selected.Where(x => x.PersonId >= 0).Concat(All.Cast<Segment>().Where(x => unknownPeople.ContainsKey(x.PersonId)));

      foreach (var segment in segments)
        ChangePerson(segment, person);

      DeselectAll();
    }

    // TODO refactoring
    /// <summary>
    /// Sets new PersonId to all Segments that are selected or that have the same PersonId (not 0) as some of the selected.
    /// The new PersonId is the highest PersonId from the selected or highest unused negative id if PersonsIds are 0.
    /// </summary>
    public void SetSelectedAsSamePerson() {
      if (Selected.Count == 0) return;

      var personsIds = Selected.Select(x => x.PersonId).Distinct().OrderByDescending(x => x).ToArray();
      // prefer known person id (id > 0)
      var newId = personsIds[0] != 0 ? personsIds[0] : personsIds.Length > 1 ? personsIds[1] : 0;

      if (newId == 0) { // get unused min ID
        var usedIds = All.Cast<Segment>().Where(x => x.PersonId < 0).
          Select(x => x.PersonId).Distinct().OrderByDescending(x => x).ToArray();
        for (var i = -1; i > usedIds.Min() - 2; i--) {
          if (usedIds.Contains(i)) continue;
          newId = i;
          break;
        }
      }

      Segment[] toUpdate;

      if (personsIds.Length == 1 && personsIds[0] == 0)
        toUpdate = Selected.ToArray();
      else {
        // take just segments with unknown people
        var allWithSameId = All.Cast<Segment>().
          Where(x => x.PersonId != 0 && x.PersonId != newId && personsIds.Contains(x.PersonId));
        toUpdate = allWithSameId.Concat(Selected.Where(x => x.PersonId == 0)).ToArray();
      }

      var person = newId < 1 ? null : Core.Instance.People.All.Find(x => x.Id == newId) as Person;

      foreach (var segment in toUpdate) {
        segment.PersonId = newId;
        segment.Person = person;
        segment.MediaItem.SetInfoBox();
        Core.Instance.Sdb.SetModified<Segments>();
      }
    }

    public void SetSelectedAsUnknown() {
      foreach (var segment in Selected) {
        RemovePersonFromSegment(segment);
        segment.PersonId = 0;
        segment.MediaItem.SetInfoBox();
        Core.Instance.Sdb.SetModified<Segments>();
      }
    }

    public void ToggleKeywordOnSelected(Keyword keyword) {
      foreach (var segment in Selected)
        ToggleKeyword(segment, keyword);
    }

    public static void ToggleKeyword(Segment segment, Keyword keyword) {
      var currentKeywords = segment.Keywords;
      Keywords.Toggle(keyword, ref currentKeywords, null, null);
      segment.Keywords = currentKeywords;
      Core.Instance.Sdb.SetModified<Segments>();
    }

    private static void RemovePersonFromSegment(Segment segment) {
      if (segment?.Person == null) return;
      if (segment.Person.Segment == segment)
        segment.Person.Segment = null;
      if (segment.Person.Segments?.Remove(segment) == true) {
        if (!segment.Person.Segments.Any())
          segment.Person.Segments = null;
        Core.Instance.Sdb.SetModified<People>();
      }
      segment.Person = null;
    }

    public void ChangePerson(int personId, Person person) {
      foreach (var segment in All.Cast<Segment>().Where(x => x.PersonId == personId))
        ChangePerson(segment, person);
    }

    public static void ChangePerson(Segment segment, Person person) {
      RemovePersonFromSegment(segment);
      segment.PersonId = person.Id;
      segment.Person = person;
      person.Segment ??= segment;
      segment.MediaItem.SetInfoBox();

      Core.Instance.Sdb.SetModified<Segments>();
    }

    public void Delete(Segment segment) {
      SetSelected(segment, false);
      RemovePersonFromSegment(segment);

      // remove Segment from MediaItem
      if (segment.MediaItem.Segments.Remove(segment) && !segment.MediaItem.Segments.Any())
        segment.MediaItem.Segments = null;

      segment.Similar?.Clear();
      segment.Picture = null;
      segment.ComparePicture = null;

      _ = All.Remove(segment);
      _ = Loaded.Remove(segment);

      foreach (var simSegment in Loaded)
        _ = simSegment.Similar?.Remove(segment);

      Core.Instance.Sdb.SetModified<Segments>();

      try {
        File.Delete(segment.CacheFilePath);
      }
      catch (Exception ex) {
        Core.Instance.LogError(ex);
      }
    }
  }
}