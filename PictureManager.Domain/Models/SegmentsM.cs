using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.EventsArgs;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsM : ObservableObject {
    private int _segmentSize = 100;
    private int _compareSegmentSize = 32;
    private int _similarityLimit = 90;
    private int _similarityLimitMin = 80;
    private bool _groupSegments;
    private bool _groupConfirmedSegments;
    private List<SegmentM> _selected = new();

    public DataAdapter DataAdapter { get; set; }
    public List<SegmentM> All { get; } = new();
    public Dictionary<int, SegmentM> AllDic { get; set; }
    public SegmentsRectsM SegmentsRectsM { get; }
    public List<SegmentM> Loaded { get; } = new();
    public List<List<SegmentM>> LoadedGroupedByPerson { get; } = new();
    public List<SegmentM> Selected => _selected;
    public ObservableCollection<SegmentM> SegmentsDrawer { get; } = new();
    public List<(int personId, SegmentM segment, List<(int personId, SegmentM segment, double sim)> similar)> ConfirmedSegments { get; } = new();
    public int SegmentSize { get => _segmentSize; set { _segmentSize = value; OnPropertyChanged(); } }
    public int CompareSegmentSize { get => _compareSegmentSize; set { _compareSegmentSize = value; OnPropertyChanged(); } }
    public int SimilarityLimit { get => _similarityLimit; set { _similarityLimit = value; OnPropertyChanged(); } }
    public int SimilarityLimitMin { get => _similarityLimitMin; set { _similarityLimitMin = value; OnPropertyChanged(); } }
    public int SelectedCount => Selected.Count;
    public bool GroupSegments { get => _groupSegments; set { _groupSegments = value; OnPropertyChanged(); } }
    public bool GroupConfirmedSegments { get => _groupConfirmedSegments; set { _groupConfirmedSegments = value; OnPropertyChanged(); } }

    public event EventHandler<SegmentPersonChangeEventArgs> SegmentPersonChangeEvent = delegate { };

    public SegmentsM() {
      SegmentsRectsM = new(this);
    }

    public void Select(List<SegmentM> list, SegmentM segment, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select(_selected, list, segment, isCtrlOn, isShiftOn, () => OnPropertyChanged(nameof(SelectedCount)));

    public void DeselectAll() =>
      Selecting.DeselectAll(_selected, () => OnPropertyChanged(nameof(SelectedCount)));

    public void SetSelected(SegmentM segment, bool value) =>
      Selecting.SetSelected(_selected, segment, value, () => OnPropertyChanged(nameof(SelectedCount)));

    public bool SegmentsDrawerToggle(SegmentM segment) {
      if (segment == null) return false;
      SegmentsDrawer.Toggle(segment);
      DataAdapter.AreTablePropsModified = true;
      return true;
    }

    private void ResetBeforeNewLoad() {
      DeselectAll();
      Loaded.Clear();
      LoadedGroupedByPerson.ForEach(x => x.Clear());
      LoadedGroupedByPerson.Clear();
    }

    public SegmentM[] GetOneOrSelected(SegmentM one) =>
      Selected.Contains(one) ? Selected.ToArray() : new[] { one };

    public SegmentM[] GetSegments(List<MediaItemM> mediaItems, bool withPersonOnly) {
      if (withPersonOnly) {
        var people = mediaItems.Where(mi => mi.Segments != null)
          .SelectMany(mi => mi.Segments.Select(s => s.PersonId)).Distinct().ToHashSet();
        people.Remove(0);
        return All.Where(s => people.Contains(s.PersonId)).OrderBy(s => s.MediaItem.FileName).ToArray();
      }

      return mediaItems.Where(x => x.Segments != null).SelectMany(x => x.Segments).ToArray();
    }

    public async IAsyncEnumerable<SegmentM> LoadSegmentsAsync(SegmentM[] segments, IProgress<int> progress, [EnumeratorCancellation] CancellationToken token = default) {
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

    public SegmentM AddNewSegment(int x, int y, int radius, MediaItemM mediaItem) {
      var newSegment = new SegmentM(DataAdapter.GetNextId(), 0, x, y, radius) { MediaItem = mediaItem };
      _ = newSegment.SetPictureAsync(SegmentSize);
      mediaItem.Segments ??= new();
      mediaItem.Segments.Add(newSegment);
      All.Add(newSegment);
      Loaded.Add(newSegment);

      return newSegment;
    }

    public SegmentM GetCopy(SegmentM s) =>
      new(DataAdapter.GetNextId(), s.PersonId, s.X, s.Y, s.Radius) {
        MediaItem = s.MediaItem,
        Person = s.Person,
        Keywords = s.Keywords?.ToList()
      };

    public async Task AddSegmentsForComparison() {
      var people = Loaded.Select(s => s.PersonId).Distinct().ToHashSet();
      people.Remove(0);
      var newSegments = All.Where(s => people.Contains(s.PersonId)).Except(Loaded);

      foreach (var segment in newSegments) {
        await segment.SetPictureAsync(SegmentSize);
        segment.MediaItem.SetThumbSize();
        Loaded.Add(segment);
      }
    }

    public Task FindSimilaritiesAsync(List<SegmentM> segments, IProgress<int> progress, CancellationToken token) {
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
      var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Name);
      var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
      var groups = groupsA.Concat(groupsB).ToArray();
      var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

      async Task<SegmentM> GetRandomSegment(IEnumerable<SegmentM> segments) {
        var tmpSegments = (segments.First().Person?.Segments ?? segments).ToArray();
        var segment = tmpSegments.ToArray()[random.Next(tmpSegments.Count() - 1)];
        await segment.SetPictureAsync(SegmentSize);
        segment.MediaItem.SetThumbSize();
        return segment;
      }

      ConfirmedSegments.Clear();

      foreach (var gA in groups) {
        (int personId, SegmentM segment, List<(int personId, SegmentM segment, double sim)> similar) confirmedSegment = new() {
          personId = gA.Key,
          segment = await GetRandomSegment(gA),
          similar = new()
        };

        ConfirmedSegments.Add(confirmedSegment);
        if (gA.All(x => x.Similar == null)) continue;

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

        List<SegmentM> segmentsGroup;

        // add segments with PersonId != 0 with all similar segments with PersonId == 0
        var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Name);
        var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
        var samePerson = groupsA.Concat(groupsB);
        foreach (var segments in samePerson) {
          var sims = new List<(SegmentM segment, double sim)>();
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
        var unknown = Loaded.Where(x => x.PersonId == 0).ToArray();
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
    /// Sets new Person to all Segments that are selected or that have the same PersonId (less than 0) as some of the selected.
    /// </summary>
    /// <param name="person"></param>
    public void SetSelectedAsPerson(PersonM person) {
      var unknownPeople = Selected.Select(x => x.PersonId).Distinct().Where(x => x < 0).ToDictionary(x => x);
      var segments = Selected.Where(x => x.PersonId >= 0).Concat(All.Where(x => unknownPeople.ContainsKey(x.PersonId)));

      foreach (var segment in segments)
        ChangePerson(segment, person, person.Id);

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
      var newId = personsIds[0] != 0
        ? personsIds[0]
        : personsIds.Length > 1
          ? personsIds[1]
          : 0;

      if (newId == 0) { // get unused min ID
        var usedIds = All.Where(x => x.PersonId < 0).
          Select(x => x.PersonId).Distinct().OrderByDescending(x => x).ToArray();
        for (var i = -1; i > usedIds.Min() - 2; i--) {
          if (usedIds.Contains(i)) continue;
          newId = i;
          break;
        }
      }

      SegmentM[] toUpdate;

      if (personsIds.Length == 1 && personsIds[0] == 0)
        toUpdate = Selected.ToArray();
      else {
        // take just segments with unknown people
        var allWithSameId = All.Where(x => x.PersonId != 0 && x.PersonId != newId && personsIds.Contains(x.PersonId));
        toUpdate = allWithSameId.Concat(Selected.Where(x => x.PersonId == 0)).ToArray();
      }

      var person = newId < 1
        ? null
        : Selected.Find(x => x.PersonId == newId)?.Person;

      foreach (var segment in toUpdate)
        ChangePerson(segment, person, newId);
    }

    public void SetSelectedAsUnknown() {
      foreach (var segment in Selected)
        ChangePerson(segment, null, 0);
    }

    public void ToggleKeywordOnSelected(KeywordM keyword) {
      foreach (var segment in Selected)
        ToggleKeyword(segment, keyword);
    }

    public void ToggleKeyword(SegmentM segment, KeywordM keyword) {
      segment.Keywords = KeywordsM.Toggle(segment.Keywords, keyword);
      DataAdapter.IsModified = true;
    }

    public void RemoveKeywordFromSegments(KeywordM keyword) {
      foreach (var segment in All.Where(x => x.Keywords?.Contains(keyword) == true))
        ToggleKeyword(segment, keyword);
    }

    public void RemovePersonFromSegments(PersonM person) {
      foreach (var segment in All.Where(s => s.Person?.Equals(person) == true)) {
        segment.Person = null;
        segment.PersonId = 0;
        DataAdapter.IsModified = true;
      }
    }

    private void ChangePerson(SegmentM segment, PersonM person, int personId) {
      SegmentPersonChangeEvent(this, new(segment, segment.Person, person));
      segment.Person = person;
      segment.PersonId = personId;
      DataAdapter.IsModified = true;
    }

    public void Delete(IEnumerable<SegmentM> segments) {
      if (segments == null) return;
      foreach (var segment in segments.ToArray())
        Delete(segment);
    }

    public void Delete(SegmentM segment) {
      SetSelected(segment, false);
      ChangePerson(segment, null, 0);

      // remove Segment from MediaItem
      if (segment.MediaItem.Segments.Remove(segment) && !segment.MediaItem.Segments.Any())
        segment.MediaItem.Segments = null;

      segment.MediaItem = null;
      segment.Similar?.Clear();
      segment.Picture = null;
      segment.ComparePicture = null;

      All.Remove(segment);
      Loaded.Remove(segment);

      foreach (var simSegment in Loaded)
        simSegment.Similar?.Remove(segment);

      try {
        File.Delete(segment.CacheFilePath);
        segment.MediaItem = null;
      }
      catch (Exception ex) {
        Core.Instance.LogError(ex);
      }
    }
  }
}