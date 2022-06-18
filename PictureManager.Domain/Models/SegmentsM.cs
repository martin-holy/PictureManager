using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.HelperClasses;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.HelperClasses;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsM : ObservableObject {
    private int _segmentSize = 100;
    private int _compareSegmentSize = 32;
    private int _similarityLimit = 90;
    private int _similarityLimitMin = 80;
    private bool _groupSegments = true;
    private bool _groupConfirmedSegments;
    private bool _matchingAutoSort = true;
    private bool _reloadAutoScroll = true;
    private readonly List<SegmentM> _selected = new();

    public SegmentsDataAdapter DataAdapter { get; set; }
    public SegmentsRectsM SegmentsRectsM { get; }
    public List<SegmentM> Loaded { get; } = new();
    public List<MediaItemM> MediaItemsForMatching { get; set; }
    public ObservableCollection<object> LoadedGrouped { get; } = new();
    public ObservableCollection<object> ConfirmedGrouped { get; } = new();
    public List<SegmentM> Selected => _selected;
    public ObservableCollection<object> SegmentsDrawer { get; } = new();
    public ObservableCollection<Tuple<int, int, int, bool>> SegmentToolTipRects { get; } = new();
    public int SegmentSize { get => _segmentSize; set { _segmentSize = value; OnPropertyChanged(); } }
    public int CompareSegmentSize { get => _compareSegmentSize; set { _compareSegmentSize = value; OnPropertyChanged(); } }
    public int SimilarityLimit { get => _similarityLimit; set { _similarityLimit = value; OnPropertyChanged(); } }
    public int SimilarityLimitMin { get => _similarityLimitMin; set { _similarityLimitMin = value; OnPropertyChanged(); } }
    public int SelectedCount => Selected.Count;
    public bool GroupSegments { get => _groupSegments; set { _groupSegments = value; OnPropertyChanged(); } }
    public bool GroupConfirmedSegments { get => _groupConfirmedSegments; set { _groupConfirmedSegments = value; OnPropertyChanged(); } }
    public bool MultiplePeopleSelected => Selected.GroupBy(x => x.Person).Count() > 1 || Selected.Count(x => x.Person == null) > 1;
    public bool MatchingAutoSort { get => _matchingAutoSort; set { _matchingAutoSort = value; OnPropertyChanged(); } }
    public bool ReloadAutoScroll { get => _reloadAutoScroll; set { _reloadAutoScroll = value; OnPropertyChanged(); } }
    public bool NeedReload { get; set; }

    public event EventHandler<ObjectEventArgs<(SegmentM, PersonM, PersonM)>> SegmentPersonChangeEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<PersonM[]>> SegmentsPersonChangedEvent = delegate { };
    public event EventHandler SegmentsKeywordChangedEvent = delegate { };
    public event EventHandler SelectedChangedEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<SegmentM>> SegmentDeletedEventHandler = delegate { };

    public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
    public RelayCommand<object> SetSelectedAsUnknownCommand { get; }
    public RelayCommand<SegmentM> SegmentToolTipReloadCommand { get; }
    public RelayCommand<object> GroupConfirmedCommand { get; }
    public RelayCommand<object> CompareAllGroupsCommand { get; }
    public RelayCommand<object> SortCommand { get; }
    public RelayCommand<object> GroupMatchingPanelCommand { get; }

    public SegmentsM() {
      SegmentsRectsM = new(this);

      SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson);
      SetSelectedAsUnknownCommand = new(SetSelectedAsUnknown);
      SegmentToolTipReloadCommand = new(SegmentToolTipReload);
      GroupConfirmedCommand = new(() => Reload(false, true));
      CompareAllGroupsCommand = new(() => LoadSegments(MediaItemsForMatching, 1));
      SortCommand = new(() => Reload(true, true));
      GroupMatchingPanelCommand = new(() => Reload(true, false));

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
      var newSegment = new SegmentM(DataAdapter.GetNextId(), x, y, radius) { MediaItem = mediaItem };
      mediaItem.Segments ??= new();
      mediaItem.Segments.Add(newSegment);
      DataAdapter.All.Add(newSegment.Id, newSegment);
      Loaded.Add(newSegment);
      NeedReload = true;

      return newSegment;
    }

    public SegmentM GetCopy(SegmentM s) =>
      new(DataAdapter.GetNextId(), s.X, s.Y, s.Radius) {
        MediaItem = s.MediaItem,
        Person = s.Person,
        Keywords = s.Keywords?.ToList()
      };

    public void AddSegmentsForComparison() {
      var people = Loaded
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .ToHashSet();
      var newSegments = DataAdapter.All.Values
        .Where(x => people.Contains(x.Person))
        .Except(Loaded);

      foreach (var segment in newSegments)
        Loaded.Add(segment);
    }

    /// <summary>
    /// Sets new Person to all Segments that are selected 
    /// or that have the same Person (with id less than 0) as some of the selected.
    /// </summary>
    /// <param name="person"></param>
    public void SetSelectedAsPerson(PersonM person) {
      var unknownPeople = Selected
        .Where(x => x.Person?.Id < 0)
        .Select(x => x.Person)
        .Distinct()
        .ToHashSet();
      var segments = Selected
        .Where(x => x.Person == null || x.Person.Id > 0)
        .Concat(DataAdapter.All.Values.Where(x => unknownPeople.Contains(x.Person)));

      MergePeople(person, unknownPeople.ToArray());

      foreach (var segment in segments)
        ChangePerson(segment, person);

      DeselectAll();

      SegmentsPersonChangedEvent(this, new(GetPeopleFromSegments(segments)));
    }

    /// <summary>
    /// Update Person TopSegments and Keywords from Persons
    /// and than remove not used Persons from DB
    /// </summary>
    /// <param name="person"></param>
    /// <param name="persons"></param>
    private void MergePeople(PersonM person, PersonM[] persons) {
      var topSegments = persons
        .Where(x => x.TopSegments != null)
        .SelectMany(x => x.TopSegments)
        .Distinct()
        .ToArray();
      var keywords = persons
        .Where(x => x.Keywords != null)
        .SelectMany(x => x.Keywords)
        .Distinct()
        .ToArray();

      if (topSegments.Any()) {
        if (person.TopSegments == null) {
          person.TopSegments = new();
          person.OnPropertyChanged(nameof(person.TopSegments));
        }
        
        foreach (var segment in topSegments)
          person.TopSegments.Add(segment);

        person.Segment = (SegmentM)person.TopSegments[0];
      }

      if (keywords.Any()) {
        person.Keywords ??= new();
        foreach (var keyword in keywords)
          person.Keywords.Add(keyword);

        person.UpdateDisplayKeywords();
      }

      foreach (var oldPerson in persons)
        Core.Instance.PeopleM.DataAdapter.All.Remove(oldPerson.Id);
    }

    /// <summary>
    /// Sets new Person to all Segments that are selected 
    /// or that have the same Person (not null) as some of the selected.
    /// The new Person is the person with the highest Id from the selected 
    /// or the newly created person with the highest unused negative id.
    /// </summary>
    private void SetSelectedAsSamePerson() {
      if (Selected.Count == 0) return;

      SegmentM[] toUpdate;
      var newPerson = Selected
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .OrderByDescending(x => x.Id)
        .FirstOrDefault();

      if (newPerson == null) {
        // create person with unused min ID
        var usedIds = DataAdapter.All.Values
          .Where(x => x.Person?.Id < 0)
          .Select(x => x.Person.Id)
          .Distinct()
          .OrderByDescending(x => x)
          .ToArray();

        for (var i = -1; i > usedIds.Min() - 2; i--) {
          if (usedIds.Contains(i)) continue;
          newPerson = new(i, $"P {i}");

          if (!Core.Instance.PeopleM.DataAdapter.All.ContainsKey(newPerson.Id))
            Core.Instance.PeopleM.DataAdapter.All.Add(newPerson.Id, newPerson);
          break;
        }

        toUpdate = Selected.ToArray();
      }
      else {
        // take just segments with unknown people
        var selectedUnknown = Selected
          .Where(x => x.Person?.Id < 0)
          .Select(x => x.Person)
          .Distinct()
          .ToHashSet();
        toUpdate = DataAdapter.All.Values
          .Where(x => x.Person?.Id < 0 && x.Person != newPerson && selectedUnknown.Contains(x.Person))
          .Concat(Selected.Where(x => x.Person == null))
          .ToArray();

        // remove not used not named people (id < 0)
        MergePeople(newPerson, selectedUnknown.Where(x => !x.Equals(newPerson)).ToArray());
      }

      foreach (var segment in toUpdate)
        ChangePerson(segment, newPerson);

      DeselectAll();
      SegmentsPersonChangedEvent(this, new(GetPeopleFromSegments(toUpdate)));
    }

    private PersonM[] GetPeopleFromSegments(IEnumerable<SegmentM> segments) =>
      segments
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .ToArray();

    private void SetSelectedAsUnknown() {
      foreach (var segment in Selected)
        ChangePerson(segment, null);

      DeselectAll();
      SegmentsPersonChangedEvent(this, new(GetPeopleFromSegments(Selected)));
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
      foreach (var segment in DataAdapter.All.Values.Where(x => x.Keywords?.Contains(keyword) == true))
        ToggleKeyword(segment, keyword);

      SegmentsKeywordChangedEvent(this, EventArgs.Empty);
    }

    public void RemovePersonFromSegments(PersonM person) {
      foreach (var segment in DataAdapter.All.Values.Where(s => s.Person?.Equals(person) == true)) {
        segment.Person = null;
        DataAdapter.IsModified = true;
      }
    }

    private void ChangePerson(SegmentM segment, PersonM person) {
      SegmentPersonChangeEventHandler(this, new((segment, segment.Person, person)));
      segment.Person = person;
      segment.MediaItem.SetInfoBox();
      DataAdapter.IsModified = true;
    }

    public void Delete(IEnumerable<SegmentM> segments) {
      if (segments == null) return;
      foreach (var segment in segments.ToArray())
        Delete(segment);
    }

    public void Delete(SegmentM segment) {
      DataAdapter.All.Remove(segment.Id);

      SegmentDeletedEventHandler(this, new(segment));
      SetSelected(segment, false);
      ChangePerson(segment, null);

      // remove Segment from MediaItem
      if (segment.MediaItem.Segments.Remove(segment) && !segment.MediaItem.Segments.Any())
        segment.MediaItem.Segments = null;

      segment.Similar?.Clear();
      
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

    private void SegmentToolTipReload(SegmentM segment) {
      SegmentToolTipRects.Clear();
      if (segment?.MediaItem?.Segments == null) return;

      segment.MediaItem.SetThumbSize();
      segment.MediaItem.SetInfoBox();

      var rotated = segment.MediaItem.Orientation is 6 or 8;
      var scale = rotated
        ? segment.MediaItem.Height / (double)segment.MediaItem.ThumbWidth
        : segment.MediaItem.Width / (double)segment.MediaItem.ThumbWidth;

      foreach (var s in segment.MediaItem.Segments) {
        var (newX, newY) = SegmentsRectsM.ConvertPos(s.X, s.Y, 1, segment.MediaItem, true);

        SegmentToolTipRects.Add(new(
          (int)((newX - s.Radius) / scale),
          (int)((newY - s.Radius) / scale),
          (int)((s.Radius * 2) / scale),
          s == segment));
      }
    }

    public List<MediaItemM> GetMediaItemsWithSegment(SegmentM segmentM, bool inGroups) {
      if (segmentM.MediaItem == null) return null;

      List<MediaItemM> items;

      if (segmentM.Person == null) {
        if (inGroups
            && LoadedGrouped.Count > 0
            && ((ItemsGroup)LoadedGrouped[^1]).Items.Cast<SegmentM>().Any(x => x.Person == null)) {
          items = ((ItemsGroup)LoadedGrouped[^1]).Items
            .Cast<SegmentM>()
            .Where(x => x.Person == null)
            .Select(x => x.MediaItem)
            .Distinct()
            .ToList();
        }
        else
          items = new() { segmentM.MediaItem };
      }
      else {
        items = DataAdapter.All.Values
          .Where(x => x.Person == segmentM.Person)
          .Select(x => x.MediaItem)
          .Distinct()
          .OrderBy(x => x.FileName)
          .ToList();
      }

      return items;
    }

    public void LoadSegments(List<MediaItemM> mediaItems, int mode) {
      ReloadAutoScroll = false;
      DeselectAll();
      Loaded.Clear();

      foreach (var segment in GetSegments(mediaItems, mode))
        Loaded.Add(segment);

      Reload(true, true);
      ReloadAutoScroll = true;
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
            .SelectMany(mi => mi.Segments
              .Where(x => x.Person != null)
              .Select(x => x.Person))
            .Distinct()
            .ToHashSet();

          return DataAdapter.All.Values
            .Where(x => x.Person != null && people.Contains(x.Person))
            .OrderBy(x => x.MediaItem.FileName)
            .ToArray();
        case 2: // one segment from each person
          return DataAdapter.All.Values
            .Where(x => x.Person != null)
            .GroupBy(x => x.Person.Id)
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

      // add segments with Person != null with all similar segments with Person == null
      string GetKeywords(SegmentM segment) =>
        segment.Keywords == null
          ? string.Empty
          : string.Join(", ", KeywordsM.GetAllKeywords(segment.Keywords).Select(k => k.Name));

      var samePerson = Loaded
        .Where(x => x.Person?.Id > 0)
        .GroupBy(x => new { x.Person, dk = GetKeywords(x) })
        .OrderBy(x => x.Key.Person.Name)
        .ThenBy(x => x.Key.dk)
        .Concat(Loaded
          .Where(x => x.Person?.Id < 0)
          .GroupBy(x => new { x.Person, dk = GetKeywords(x) })
          .OrderByDescending(x => x.Key.Person.Id)
          .ThenBy(x => x.Key.dk));

      foreach (var segments in samePerson) {
        var sims = new List<(SegmentM segment, double sim)>();

        group = new();
        group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, segments.Key.Person.Name));
        if (!segments.Key.dk.Equals(string.Empty))
          group.Info.Add(new ItemsGroupInfoItem(Res.IconTag, segments.Key.dk));
        LoadedGrouped.Add(group);

        foreach (var segment in segments.OrderBy(x => x.MediaItem.FileName)) {
          group.Items.Add(segment);
          if (segment.Similar == null) continue;
          sims.AddRange(segment.Similar
            .Where(x => x.Key.Person == null && x.Value >= SimilarityLimit)
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

      // add segments with Person == null ordered by similar
      var unknown = Loaded.Where(x => x.Person == null).ToArray();
      if (unknown.Length == 0) return;
      var set = new HashSet<int>();
      var withSimilar = unknown
        .Where(x => x.Similar != null)
        .OrderByDescending(x => x.SimMax);

      group = new();
      group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, "0"));
      LoadedGrouped.Add(group);

      foreach (var segment in withSimilar) {
        var simSegments = segment.Similar
          .Where(x => x.Key.Person == null && x.Value >= SimilarityLimit)
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
    /// Compares segments with same person to other segments with same person
    /// and select top segment from each person for display
    /// </summary>
    private void ReloadConfirmedGrouped() {
      var groups = Loaded
        .Where(x => x.Person?.Id > 0)
        .GroupBy(x => x.Person)
        .OrderBy(x => x.First().Person.Name)
        .Concat(Loaded
          .Where(x => x.Person?.Id < 0)
          .GroupBy(x => x.Person)
          .OrderByDescending(x => x.Key.Id))
        .ToArray();

      var tmp = new List<(PersonM person, SegmentM segment, List<(PersonM person, SegmentM segment, double sim)> similar)>();

      SegmentM GetTopSegment(IEnumerable<SegmentM> segments) =>
        (segments.First().Person?.TopSegments?.Cast<SegmentM>() ?? segments).First();

      ConfirmedGrouped.Clear();

      // get segments
      foreach (var gA in groups) {
        (PersonM person, SegmentM segment, List<(PersonM person, SegmentM segment, double sim)> similar) confirmedSegment = new() {
          person = gA.Key,
          segment = GetTopSegment(gA),
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
          confirmedSegment.similar.Add(new(gB.Key, GetTopSegment(gB), simMedian));
        }
      }

      if (GroupConfirmedSegments) {
        foreach (var (_, segment, similar) in tmp) {
          var group = new ItemsGroup();
          group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, segment.Person.Name));
          ConfirmedGrouped.Add(group);
          group.Items.Add(segment);

          foreach (var simGroup in similar.OrderByDescending(x => x.sim))
            group.Items.Add(simGroup.segment);
        }
      }
      else {
        const string zzzz = "zzzz";
        foreach (var group in tmp
          .GroupBy(x => x.person.DisplayKeywords == null
            ? zzzz
            : string.Join(", ", x.segment.Person.DisplayKeywords.Select(k => k.Name)))
          .OrderBy(g => g.Key)
          .ThenBy(g => g.First().person.Name)) {

          var itemsGroup = new ItemsGroup();
          var infoItem = group.Key.Equals(zzzz)
            ? new ItemsGroupInfoItem(Res.IconEmpty, string.Empty)
            : new ItemsGroupInfoItem(Res.IconTag, group.Key);
          itemsGroup.Info.Add(infoItem);
          ConfirmedGrouped.Add(itemsGroup);

          foreach (var (_, segment, _) in group)
            itemsGroup.Items.Add(segment);
        }
      }
    }
  }
}