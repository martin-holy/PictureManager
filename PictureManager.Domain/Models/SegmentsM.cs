using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.HelperClasses;
using PictureManager.Domain.HelperClasses;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsM : ObservableObject {
    private int _segmentSize = 100;
    private int _compareSegmentSize = 32;
    private int _similarityLimit = 90;
    private int _similarityLimitMin = 80;
    private bool _groupSegments;
    private bool _groupConfirmedSegments;
    private bool _matchingAutoSort = true;
    private readonly List<SegmentM> _selected = new();
    private readonly IProgress<int> _progress;

    public DataAdapter DataAdapter { get; set; }
    public List<SegmentM> All { get; } = new();
    public Dictionary<int, SegmentM> AllDic { get; set; }
    public SegmentsRectsM SegmentsRectsM { get; }
    public List<SegmentM> Loaded { get; } = new();
    public ObservableCollection<object> LoadedGrouped { get; } = new();
    public ObservableCollection<object> ConfirmedGrouped { get; } = new();
    public List<SegmentM> Selected => _selected;
    public ObservableCollection<object> SegmentsDrawer { get; } = new();
    public ObservableCollection<Tuple<int, int, int, int, bool>> SegmentToolTipRects { get; } = new();
    public int SegmentSize { get => _segmentSize; set { _segmentSize = value; OnPropertyChanged(); } }
    public int CompareSegmentSize { get => _compareSegmentSize; set { _compareSegmentSize = value; OnPropertyChanged(); } }
    public int SimilarityLimit { get => _similarityLimit; set { _similarityLimit = value; OnPropertyChanged(); } }
    public int SimilarityLimitMin { get => _similarityLimitMin; set { _similarityLimitMin = value; OnPropertyChanged(); } }
    public int SelectedCount => Selected.Count;
    public bool GroupSegments { get => _groupSegments; set { _groupSegments = value; OnPropertyChanged(); } }
    public bool GroupConfirmedSegments { get => _groupConfirmedSegments; set { _groupConfirmedSegments = value; OnPropertyChanged(); } }
    public bool MultiplePeopleSelected => Selected.GroupBy(x => x.PersonId).Count() > 1 || Selected.Count(x => x.PersonId == 0) > 1;
    public bool MatchingAutoSort { get => _matchingAutoSort; set { _matchingAutoSort = value; OnPropertyChanged(); } }

    public event EventHandler<ObjectEventArgs<(SegmentM, PersonM, PersonM)>> SegmentPersonChangeEventHandler = delegate { };
    public event EventHandler SegmentsPersonChangedEvent = delegate { };
    public event EventHandler SegmentsKeywordChangedEvent = delegate { };
    public event EventHandler SelectedChangedEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<SegmentM>> SegmentDeletedEventHandler = delegate { };

    public static Func<SegmentM, int, Task> SetComparePictureAsync { get; set; }

    public SegmentsM() {
      SegmentsRectsM = new(this);

      _progress = new Progress<int>(x => {
        Core.Instance.TitleProgressBarM.ValueA = x;
        Core.Instance.TitleProgressBarM.ValueB = x;
      });

      SelectedChangedEventHandler += (_, _) => {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(MultiplePeopleSelected));
      };
    }

    public void Select(List<SegmentM> list, SegmentM segment, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select(_selected, list, segment, isCtrlOn, isShiftOn, () => SelectedChangedEventHandler(this, EventArgs.Empty));

    public void DeselectAll() =>
      Selecting.DeselectAll(_selected, () => SelectedChangedEventHandler(this, EventArgs.Empty));

    public void SetSelected(SegmentM segment, bool value) =>
      Selecting.SetSelected(_selected, segment, value, () => SelectedChangedEventHandler(this, EventArgs.Empty));

    public void SegmentsDrawerUpdate(SegmentM[] segments, bool add) {
      if (!add && Core.DialogHostShow(new MessageDialog(
            "Segments Drawer",
            "Do you want to remove segments from drawer?",
            Res.IconQuestion,
            true)) != 0)
        return;

      var count = SegmentsDrawer.Count;

      if (add)
        foreach (var segment in segments.Except(SegmentsDrawer).ToArray())
          SegmentsDrawer.Add(segment);
      else
        foreach (var segment in segments)
          SegmentsDrawer.Remove(segment);

      if (count != SegmentsDrawer.Count)
        DataAdapter.AreTablePropsModified = true;
    }

    public void SegmentsDrawerRemove(SegmentM segment) {
      if (SegmentsDrawer.Remove(segment))
        DataAdapter.AreTablePropsModified = true;
    }

    public SegmentM[] GetOneOrSelected(SegmentM one) =>
      Selected.Contains(one)
        ? Selected.ToArray()
        : new[] { one };

    public SegmentM AddNewSegment(int x, int y, int radius, MediaItemM mediaItem) {
      var newSegment = new SegmentM(DataAdapter.GetNextId(), 0, x, y, radius) { MediaItem = mediaItem };
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

    public void AddSegmentsForComparison() {
      var people = Loaded.Select(s => s.PersonId).Distinct().ToHashSet();
      people.Remove(0);
      var newSegments = All.Where(s => people.Contains(s.PersonId)).Except(Loaded);

      foreach (var segment in newSegments)
        Loaded.Add(segment);
    }

    public Task FindSimilaritiesAsync(List<SegmentM> segments, CancellationToken token) {
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
          await SetComparePictureAsync(segmentA, CompareSegmentSize);
          _progress.Report(++done);
        }

        done = 0;
        _progress.Report(done);

        foreach (var segmentA in segments) {
          if (token.IsCancellationRequested) break;
          if (segmentA.ComparePicture == null) {
            _progress.Report(++done);
            continue;
          }

          segmentA.Similar ??= new();
          foreach (var segmentB in segments) {
            if (token.IsCancellationRequested) break;
            if (segmentB.ComparePicture == null) continue;
            // do not compare segment with it self or with segment that have same person
            if (segmentA == segmentB || (segmentA.PersonId != 0 && segmentA.PersonId == segmentB.PersonId)) continue;
            // do not compare segment with PersonId > 0 with segment with also PersonId > 0
            if (segmentA.PersonId > 0 && segmentB.PersonId > 0) continue;

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

          _progress.Report(++done);
        }
      }, token);
    }

    public void SetPerson(PersonM person) {
      if (Selected.Count == 0) return;

      var msgCount = Selected.Count > 1 ? $"'s ({Selected.Count})" : string.Empty;
      var msg = $"Do you want to set ({person.Name}) to selected segment{msgCount}??";

      if (Core.DialogHostShow(new MessageDialog("Set Person", msg, Res.IconQuestion, true)) == 0)
        SetSelectedAsPerson(person);
    }

    /// <summary>
    /// Sets new Person to all Segments that are selected or that have the same PersonId (less than 0) as some of the selected.
    /// </summary>
    /// <param name="person"></param>
    private void SetSelectedAsPerson(PersonM person) {
      var unknownPeople = Selected.Select(x => x.PersonId).Distinct().Where(x => x < 0).ToDictionary(x => x);
      var segments = Selected.Where(x => x.PersonId >= 0).Concat(All.Where(x => unknownPeople.ContainsKey(x.PersonId)));

      foreach (var segment in segments)
        ChangePerson(segment, person, person.Id);

      DeselectAll();

      SegmentsPersonChangedEvent(this, EventArgs.Empty);
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

      DeselectAll();
      SegmentsPersonChangedEvent(this, EventArgs.Empty);
    }

    public void SetSelectedAsUnknown() {
      foreach (var segment in Selected)
        ChangePerson(segment, null, 0);

      DeselectAll();
      SegmentsPersonChangedEvent(this, EventArgs.Empty);
    }

    public void ToggleKeywordOnSelected(KeywordM keyword) {
      foreach (var segment in Selected)
        ToggleKeyword(segment, keyword);

      SegmentsKeywordChangedEvent(this, EventArgs.Empty);
    }

    private void ToggleKeyword(SegmentM segment, KeywordM keyword) {
      segment.Keywords = KeywordsM.Toggle(segment.Keywords, keyword);
      DataAdapter.IsModified = true;
    }

    public void RemoveKeywordFromSegments(KeywordM keyword) {
      foreach (var segment in All.Where(x => x.Keywords?.Contains(keyword) == true))
        ToggleKeyword(segment, keyword);

      SegmentsKeywordChangedEvent(this, EventArgs.Empty);
    }

    public void RemovePersonFromSegments(PersonM person) {
      foreach (var segment in All.Where(s => s.Person?.Equals(person) == true)) {
        segment.Person = null;
        segment.PersonId = 0;
        DataAdapter.IsModified = true;
      }
    }

    private void ChangePerson(SegmentM segment, PersonM person, int personId) {
      SegmentPersonChangeEventHandler(this, new((segment, segment.Person, person)));
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
      SegmentDeletedEventHandler(this, new(segment));
      SetSelected(segment, false);
      ChangePerson(segment, null, 0);

      // remove Segment from MediaItem
      if (segment.MediaItem.Segments.Remove(segment) && !segment.MediaItem.Segments.Any())
        segment.MediaItem.Segments = null;

      segment.Similar?.Clear();
      segment.ComparePicture = null;

      All.Remove(segment);
      if (Loaded.Remove(segment))
        Reload();

      foreach (var simSegment in Loaded)
        simSegment.Similar?.Remove(segment);

      try {
        File.Delete(segment.FilePathCache);
        segment.MediaItem = null;
      }
      catch (Exception ex) {
        Core.Instance.LogError(ex);
      }
    }

    public void SegmentToolTipReload(SegmentM segment) {
      SegmentToolTipRects.Clear();
      if (segment?.MediaItem?.Segments == null) return;

      segment.MediaItem.SetThumbSize();
      segment.MediaItem.SetInfoBox();

      var scale = segment.MediaItem.Width / (double)segment.MediaItem.ThumbWidth;

      foreach (var s in segment.MediaItem.Segments)
        SegmentToolTipRects.Add(new(
          (int)((s.X - s.Radius) / scale),
          (int)((s.Y - s.Radius) / scale),
          (int)((s.Radius * 2) / scale),
          (int)((s.Radius * 2) / scale),
          s == segment));
    }

    public List<MediaItemM> GetMediaItemsWithSegment(SegmentM segmentM, bool inGroups) {
      if (segmentM.MediaItem == null) return null;

      List<MediaItemM> items;

      if (segmentM.PersonId == 0) {
        if (inGroups
            && LoadedGrouped.Count > 0
            && ((ItemsGroup)LoadedGrouped[^1]).Items.Cast<SegmentM>().Any(x => x.PersonId == 0)) {
          items = ((ItemsGroup)LoadedGrouped[^1]).Items
            .Cast<SegmentM>()
            .Where(x => x.PersonId == 0)
            .Select(x => x.MediaItem)
            .Distinct()
            .ToList();
        }
        else
          items = new() { segmentM.MediaItem };
      }
      else {
        items = All
          .Where(x => x.PersonId == segmentM.PersonId)
          .Select(x => x.MediaItem)
          .Distinct()
          .OrderBy(x => x.FileName)
          .ToList();
      }

      return items;
    }

    public void ReloadPersonSegments(PersonM person, List<SegmentM> allSegments, ObservableCollection<object> allSegmentsGrouped) {
      allSegments.Clear();
      allSegmentsGrouped.Clear();

      if (person == null) return;

      foreach (var group in All
        .Where(x => x.PersonId == person.Id)
        .GroupBy(x => x.Keywords == null
          ? string.Empty
          : string.Join(", ", KeywordsM.GetAllKeywords(x.Keywords).Select(k => k.Name)))
        .OrderBy(x => x.Key)) {

        if (string.IsNullOrEmpty(group.Key)) {
          // add segments without group
          foreach (var segment in group.OrderBy(x => x.MediaItem.FileName)) {
            allSegments.Add(segment);
            allSegmentsGrouped.Add(segment);
          }
        }
        else {
          // add segments in group
          var itemsGroup = new ItemsGroup();
          itemsGroup.Info.Add(new ItemsGroupInfoItem { Icon = Res.IconTag, Title = group.Key });
          allSegmentsGrouped.Add(itemsGroup);

          foreach (var segment in group.OrderBy(x => x.MediaItem.FileName)) {
            allSegments.Add(segment);
            itemsGroup.Items.Add(segment);
          }
        }
      }
    }

    public void LoadSegments(List<MediaItemM> mediaItems, int mode) {
      GroupSegments = false;
      DeselectAll();
      Loaded.Clear();

      foreach (var segment in GetSegments(mediaItems, mode))
        Loaded.Add(segment);

      Reload(true, true);
    }

    public void Reload() =>
      Reload(MatchingAutoSort, MatchingAutoSort);

    public void Reload(bool segments, bool confirmedSegments) {
      if (segments)
        ReloadLoadedGrouped();

      if (confirmedSegments)
        ReloadConfirmedGrouped();
    }

    private SegmentM[] GetSegments(List<MediaItemM> mediaItems, int mode) {
      switch (mode) {
        case 0: // all segments from mediaItems
          return mediaItems
            .Where(x => x.Segments != null)
            .SelectMany(x => x.Segments)
            .ToArray();
        case 1: // all segments with person found on segments from mediaItems
          var people = mediaItems
            .Where(mi => mi.Segments != null)
            .SelectMany(mi => mi.Segments.Select(s => s.PersonId))
            .Distinct()
            .ToHashSet();
          people.Remove(0);

          return All
            .Where(s => people.Contains(s.PersonId))
            .OrderBy(s => s.MediaItem.FileName)
            .ToArray();
        case 2: // one segment from each person
          return All
            .Where(x => x.PersonId != 0)
            .GroupBy(x => x.PersonId)
            .Select(x => x.First())
            .ToArray();
        default:
          return Array.Empty<SegmentM>();
      }
    }

    private void ReloadLoadedGrouped() {
      ItemsGroup group;
      LoadedGrouped.Clear();

      if (!GroupSegments) {
        group = new();
        group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, "?"));
        LoadedGrouped.Add(group);

        foreach (var segment in Loaded)
          group.Items.Add(segment);

        group.Info.Add(new ItemsGroupInfoItem(Res.IconImageMultiple, group.Items.Count.ToString()));

        return;
      }

      // add segments with PersonId != 0 with all similar segments with PersonId == 0
      var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Name);
      var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
      var samePerson = groupsA.Concat(groupsB);
      foreach (var segments in samePerson) {
        var sims = new List<(SegmentM segment, double sim)>();
        var s = segments.First();
        var groupTitle = s.Person != null
            ? s.Person.Name
            : s.PersonId.ToString();

        group = new();
        group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, groupTitle));
        LoadedGrouped.Add(group);

        foreach (var segment in segments.OrderBy(x => x.MediaItem.FileName)) {
          group.Items.Add(segment);
          if (segment.Similar == null) continue;
          sims.AddRange(segment.Similar
            .Where(x => x.Key.PersonId == 0 && x.Value >= SimilarityLimit)
            .Select(x => (x.Key, x.Value)));
        }

        // order by number of similar than by similarity
        foreach (var segment in sims
                   .GroupBy(x => x.segment)
                   .OrderByDescending(g => g.Count())
                   .Select(g => g.OrderByDescending(x => x.sim).First().segment)) {
          group.Items.Add(segment);
        }

        group.Info.Add(new ItemsGroupInfoItem(Res.IconImageMultiple, group.Items.Count.ToString()));
      }

      // add segments with PersonId == 0 ordered by similar
      var unknown = Loaded.Where(x => x.PersonId == 0).ToArray();
      var set = new HashSet<int>();
      var withSimilar = unknown
        .Where(x => x.Similar != null)
        .OrderByDescending(x => x.SimMax);

      group = new();
      group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, "0"));
      LoadedGrouped.Add(group);

      foreach (var segment in withSimilar) {
        var simSegments = segment.Similar
          .Where(x => x.Key.PersonId == 0 && x.Value >= SimilarityLimit)
          .OrderByDescending(x => x.Value);

        foreach (var simSegment in simSegments) {
          if (set.Add(segment.Id)) group.Items.Add(segment);
          if (set.Add(simSegment.Key.Id)) group.Items.Add(simSegment.Key);
        }
      }

      // add rest of the segments
      foreach (var segment in unknown.Where(x => !set.Contains(x.Id)))
        group.Items.Add(segment);

      group.Info.Add(new ItemsGroupInfoItem(Res.IconImageMultiple, group.Items.Count.ToString()));
    }

    /// <summary>
    /// Compares segments with same person id to other segments with same person id
    /// and select random segment from each group for display
    /// </summary>
    private void ReloadConfirmedGrouped() {
      var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Name);
      var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
      var groups = groupsA.Concat(groupsB).ToArray();
      var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
      var tmp = new List<(int personId, SegmentM segment, List<(int personId, SegmentM segment, double sim)> similar)>();

      SegmentM GetRandomSegment(IEnumerable<SegmentM> segments) {
        var tmpSegments = (segments.First().Person?.TopSegments?.Cast<SegmentM>() ?? segments).ToArray();
        var segment = tmpSegments.ToArray()[random.Next(tmpSegments.Length - 1)];
        return segment;
      }

      ConfirmedGrouped.Clear();

      // get segments
      foreach (var gA in groups) {
        (int personId, SegmentM segment, List<(int personId, SegmentM segment, double sim)> similar) confirmedSegment = new() {
          personId = gA.Key,
          segment = GetRandomSegment(gA),
          similar = new()
        };

        tmp.Add(confirmedSegment);
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
          confirmedSegment.similar.Add(new(gB.Key, GetRandomSegment(gB), simMedian));
        }
      }

      if (GroupConfirmedSegments) {
        foreach (var (personId, segment, similar) in tmp) {
          var groupTitle = segment.Person != null
            ? segment.Person.Name
            : personId.ToString();

          var group = new ItemsGroup();
          group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, groupTitle));
          ConfirmedGrouped.Add(group);
          group.Items.Add(segment);

          foreach (var simGroup in similar.OrderByDescending(x => x.sim))
            group.Items.Add(simGroup.segment);
        }
      }
      else {
        foreach (var group in tmp
          .GroupBy(x => {
            if (x.segment.Person == null) return "Unknown";
            return x.segment.Person.DisplayKeywords == null
              ? string.Empty
              : string.Join(", ", x.segment.Person.DisplayKeywords.Select(k => k.Name));
          })
          .OrderBy(g => g.First().personId < 0)
          .ThenBy(g => g.Key)) {

          if (string.IsNullOrEmpty(group.Key)) {
            foreach (var (_, segment, _) in group)
              ConfirmedGrouped.Add(segment);
          }
          else {
            var itemsGroup = new ItemsGroup();
            itemsGroup.Info.Add(new ItemsGroupInfoItem(
              "Unknown".Equals(group.Key)
                ? Res.IconPeople
                : Res.IconTag,
              group.Key));
            ConfirmedGrouped.Add(itemsGroup);

            foreach (var (_, segment, _) in group)
              itemsGroup.Items.Add(segment);
          }
        }
      }
    }
  }
}