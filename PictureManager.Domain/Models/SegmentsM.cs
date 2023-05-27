using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.HelperClasses;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.HelperClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsM : ObservableObject {
    private readonly Core _core;
    private double _segmentUiSize;
    private int _segmentSize = 100;
    private int _compareSegmentSize = 32;
    private int _similarityLimit = 90;
    private int _similarityLimitMin = 80;
    private bool _groupSegments = true;
    private bool _groupConfirmedSegments;
    private bool _canSelectAsSamePerson;
    private bool _matchingAutoSort = true;
    private bool _reloadAutoScroll = true;

    public HeaderedListItem<object, string> MainTabsItem { get; set; }
    public SegmentsDataAdapter DataAdapter { get; set; }
    public SegmentsRectsM SegmentsRectsM { get; }
    public SegmentsDrawerM SegmentsDrawerM { get; }
    public List<SegmentM> Loaded { get; } = new();
    public List<MediaItemM> MediaItemsForMatching { get; set; }
    public ObservableCollection<object> LoadedGrouped { get; } = new();
    public ObservableCollection<object> ConfirmedGrouped { get; } = new();
    public Selecting<SegmentM> Selected { get; } = new();
    public int SegmentSize { get => _segmentSize; set { _segmentSize = value; OnPropertyChanged(); } }
    public int CompareSegmentSize { get => _compareSegmentSize; set { _compareSegmentSize = value; OnPropertyChanged(); } }
    public int SimilarityLimit { get => _similarityLimit; set { _similarityLimit = value; OnPropertyChanged(); } }
    public int SimilarityLimitMin { get => _similarityLimitMin; set { _similarityLimitMin = value; OnPropertyChanged(); } }
    public bool GroupSegments { get => _groupSegments; set { _groupSegments = value; OnPropertyChanged(); } }
    public bool GroupConfirmedSegments { get => _groupConfirmedSegments; set { _groupConfirmedSegments = value; OnPropertyChanged(); } }
    public bool CanSelectAsSamePerson { get => _canSelectAsSamePerson; set { _canSelectAsSamePerson = value; OnPropertyChanged(); } }
    public bool MatchingAutoSort { get => _matchingAutoSort; set { _matchingAutoSort = value; OnPropertyChanged(); } }
    public bool ReloadAutoScroll { get => _reloadAutoScroll; set { _reloadAutoScroll = value; OnPropertyChanged(); } }
    public bool NeedReload { get; set; }
    public double ConfirmedPanelWidth { get; private set; }
    public double SegmentUiFullWidth { get; set; }
    public double SegmentUiSize { get => _segmentUiSize; set { _segmentUiSize = value; OnPropertyChanged(); } }

    public CanDragFunc CanDragFunc { get; }

    public event EventHandler<ObjectEventArgs<(SegmentM, PersonM, PersonM)>> SegmentPersonChangeEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<(SegmentM[], PersonM[])>> SegmentsPersonChangedEvent = delegate { };
    public event EventHandler<ObjectEventArgs<(SegmentM[], KeywordM)>> SegmentsKeywordChangedEvent = delegate { };
    public event EventHandler<ObjectEventArgs<SegmentM>> SegmentDeletedEventHandler = delegate { };

    public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
    public RelayCommand<object> SetSelectedAsUnknownCommand { get; }
    public RelayCommand<object> GroupConfirmedCommand { get; }
    public RelayCommand<object> CompareAllGroupsCommand { get; }
    public RelayCommand<object> SortCommand { get; }
    public RelayCommand<object> GroupMatchingPanelCommand { get; }
    public RelayCommand<SegmentM> ViewMediaItemsWithSegmentCommand { get; }
    public RelayCommand<object> SegmentMatchingCommand { get; }

    public SegmentsM(Core core) {
      _core = core;
      SegmentsRectsM = new(this);
      SegmentsDrawerM = new(this, _core);

      SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson);
      SetSelectedAsUnknownCommand = new(SetSelectedAsUnknown, () => Selected.Items.Count > 0);
      GroupConfirmedCommand = new(() => Reload(false, true));
      CompareAllGroupsCommand = new(() => LoadSegments(MediaItemsForMatching, 1));
      SortCommand = new(() => Reload(true, true));
      GroupMatchingPanelCommand = new(() => Reload(true, false));
      ViewMediaItemsWithSegmentCommand = new(ViewMediaItemsWithSegment);
      SegmentMatchingCommand = new(
        SegmentMatching,
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);

      CanDragFunc = CanDrag;
    }

    public void Select(List<SegmentM> segments, SegmentM segment, bool isCtrlOn, bool isShiftOn) {
      if (!isCtrlOn && !isShiftOn)
        _core.PeopleM.Selected.DeselectAll();

      Selected.Select(segments, segment, isCtrlOn, isShiftOn);
      _core.PeopleM.Selected.Add(Selected.Items
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct());
      SetCanSelectAsSamePerson();
    }

    private object CanDrag(object source) =>
      source is SegmentM segmentM
        ? GetOneOrSelected(segmentM)
        : null;

    public void SetCanSelectAsSamePerson() {
      CanSelectAsSamePerson =
        Selected.Items.GroupBy(x => x.Person).Count() > 1
        || Selected.Items.Count(x => x.Person == null) > 1;
    }

    public void SetSegmentUiSize(double size, double scrollBarSize) {
      SegmentUiSize = size;
      SegmentUiFullWidth = size + 6; // + border, margin
      ConfirmedPanelWidth = (SegmentUiFullWidth * 2) + scrollBarSize;
    }

    public SegmentM[] GetOneOrSelected(SegmentM one) =>
      Selected.Items.Contains(one)
        ? Selected.Items.ToArray()
        : new[] { one };

    public SegmentM AddNewSegment(double x, double y, int size, MediaItemM mediaItem) {
      var newSegment = new SegmentM(DataAdapter.GetNextId(), x, y, size) { MediaItem = mediaItem };
      mediaItem.Segments ??= new();
      mediaItem.Segments.Add(newSegment);
      DataAdapter.All.Add(newSegment);
      Loaded.Add(newSegment);
      NeedReload = true;

      return newSegment;
    }

    public SegmentM GetCopy(SegmentM s) =>
      new(DataAdapter.GetNextId(), s.X, s.Y, s.Size) {
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
      var newSegments = DataAdapter.All
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
      var unknownPeople = Selected.Items
        .Where(x => x.Person?.Id < 0)
        .Select(x => x.Person)
        .Distinct()
        .ToHashSet();
      var segments = Selected.Items
        .Where(x => x.Person == null || x.Person.Id > 0)
        .Concat(DataAdapter.All.Where(x => unknownPeople.Contains(x.Person)))
        .ToArray();
      var people = segments
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Concat(new[] { person })
        .Distinct()
        .ToArray();

      MergePeople(person, unknownPeople.ToArray());

      foreach (var segment in segments)
        ChangePerson(segment, person);

      Selected.DeselectAll();

      SegmentsPersonChangedEvent(this, new((segments, people)));
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

      if (persons.Contains(_core.PersonDetailM.PersonM))
        _core.PersonDetailM.PersonM = person;

      foreach (var oldPerson in persons)
        Core.Instance.PeopleM.DataAdapter.All.Remove(oldPerson);
    }

    /// <summary>
    /// Sets new Person to all Segments that are selected 
    /// or that have the same Person (not null) as some of the selected.
    /// The new Person is the person with the highest Id from the selected 
    /// or the newly created person with the highest unused negative id.
    /// </summary>
    private void SetSelectedAsSamePerson() {
      if (!CanSelectAsSamePerson) return;

      PersonM newPerson = null;
      SegmentM[] toUpdate;
      var people = Selected.Items
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .OrderBy(x => x.Name)
        .ToArray();

      if (people.Length == 0) {
        // create person with unused min ID
        var usedIds = DataAdapter.All
          .Where(x => x.Person?.Id < 0)
          .Select(x => x.Person.Id)
          .Distinct()
          .OrderByDescending(x => x)
          .ToArray();

        for (var i = -1; i > usedIds.Min() - 2; i--) {
          if (usedIds.Contains(i)) continue;
          newPerson = new(i, $"P {i}");
          _core.PeopleM.DataAdapter.All.Add(newPerson);

          break;
        }

        toUpdate = Selected.Items.ToArray();
      }
      else {
        if (people.Length == 1)
          newPerson = people[0];
        else {
          var spd = new SetSegmentPersonDialogM(this, people);
          if (Core.DialogHostShow(spd) != 1) return;
          newPerson = spd.Person;
          toUpdate = spd.Segments;
        }

        toUpdate = GetSegmentsToUpdate(newPerson, people);
        MergePeople(newPerson, people.Where(x => !x.Equals(newPerson)).ToArray());
      }

      var affectedPeople = people.Concat(new[] { newPerson }).Distinct().ToArray();

      foreach (var segment in toUpdate)
        ChangePerson(segment, newPerson);

      Selected.DeselectAll();
      _core.PeopleM.Selected.DeselectAll();
      SegmentsPersonChangedEvent(this, new((toUpdate, affectedPeople)));
    }

    public SegmentM[] GetSegmentsToUpdate(PersonM person, IEnumerable<PersonM> people) {
      var oldPeople = people.Where(x => !x.Equals(person)).ToHashSet();
      return DataAdapter.All
        .Where(x => oldPeople.Contains(x.Person))
        .Concat(Selected.Items.Where(x => x.Person == null))
        .ToArray();
    }

    private PersonM[] GetPeopleFromSegments(IEnumerable<SegmentM> segments) =>
      segments
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .ToArray();

    private void SetSelectedAsUnknown() {
      var msgCount = Selected.Items.Count == 1
        ? "selected segment"
        : $"{Selected.Items.Count} selected segments";
      var msg = $"Do you want to set {msgCount} as unknown?";

      if (Core.DialogHostShow(new MessageDialog("Set as unknown", msg, Res.IconQuestion, true)) != 1)
        return;

      var segments = Selected.Items.ToArray();
      var people = segments
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .ToArray();
      foreach (var segment in segments)
        ChangePerson(segment, null);

      Selected.DeselectAll();
      SegmentsPersonChangedEvent(this, new((segments, people)));
    }

    private void ToggleKeyword(SegmentM segment, KeywordM keyword) {
      segment.Keywords = KeywordsM.Toggle(segment.Keywords, keyword);
      DataAdapter.IsModified = true;
    }

    private void ToggleKeyword(IEnumerable<SegmentM> segments, KeywordM keyword) {
      foreach (var segment in segments)
        ToggleKeyword(segment, keyword);

      SegmentsKeywordChangedEvent(this, new((segments.ToArray(), keyword)));
    }

    public void RemoveKeywordFromSegments(KeywordM keyword) =>
      ToggleKeyword(DataAdapter.All.Where(x => x.Keywords?.Contains(keyword) == true), keyword);

    public void ToggleKeywordOnSelected(KeywordM keyword) =>
      ToggleKeyword(Selected.Items, keyword);

    public void RemovePersonFromSegments(PersonM person) {
      foreach (var segment in DataAdapter.All.Where(s => s.Person?.Equals(person) == true)) {
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
      DataAdapter.All.Remove(segment);

      SegmentDeletedEventHandler(this, new(segment));
      Selected.Set(segment, false);
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
        if (File.Exists(segment.FilePathCache))
          File.Delete(segment.FilePathCache);

        segment.MediaItem = null;
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }

    public List<MediaItemM> GetMediaItemsWithSegment(SegmentM segmentM, bool inGroups) {
      if (segmentM.MediaItem == null) return null;

      if (segmentM.Person != null)
        return DataAdapter.All
          .Where(x => x.Person == segmentM.Person)
          .Select(x => x.MediaItem)
          .Distinct()
          .OrderBy(x => x.FileName)
          .ToList();

      if (SegmentsDrawerM.Items.Contains(segmentM))
        return SegmentsDrawerM.Items.Select(x => x.MediaItem).OrderBy(x => x.FilePath).ToList();

      if (inGroups) {
        var items = ((ItemsGroup)LoadedGrouped[^1]).Items
          .Cast<SegmentM>()
          .Where(x => x.Person == null)
          .Select(x => x.MediaItem)
          .Distinct();

        if (items.Any())
          return items.ToList();
      }

      return null;
    }

    public void LoadSegments(List<MediaItemM> mediaItems, int mode) {
      ReloadAutoScroll = false;
      Selected.DeselectAll();
      Loaded.Clear();

      foreach (var segment in GetSegments(mediaItems, mode))
        Loaded.Add(segment);

      Reload(true, true);
      ReloadAutoScroll = true;
    }

    public void ReloadIfContains(IEnumerable<SegmentM> segments) {
      if (Loaded.Any(x => segments.Equals(x)))
        Reload();
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
        case 1: // all segments from mediaItems
          return mediaItems
            .Where(x => x.Segments != null)
            .SelectMany(x => x.Segments)
            .ToArray();
        case 2: // all segments with person found on segments from mediaItems
          var people = mediaItems
            .Where(mi => mi.Segments != null)
            .SelectMany(mi => mi.Segments
              .Where(x => x.Person != null)
              .Select(x => x.Person))
            .Distinct()
            .ToHashSet();

          return DataAdapter.All
            .Where(x => x.Person != null && people.Contains(x.Person))
            .OrderBy(x => x.MediaItem.FileName)
            .ToArray();
        case 3: // one segment from each person
          return DataAdapter.All
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
        string.Join(", ", KeywordsM.GetAllKeywords(segment.Keywords).Select(k => k.Name));

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
          if (set.Add(segment.GetHashCode())) group.Items.Add(segment);
          if (set.Add(simSegment.Key.GetHashCode())) group.Items.Add(simSegment.Key);
        }
      }

      // add rest of the segments
      foreach (var segment in unknown.Where(x => !set.Contains(x.GetHashCode())))
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
        foreach (var (person, _, similar) in tmp) {
          var group = new ItemsGroup();
          group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, person.Name));
          ConfirmedGrouped.Add(group);
          group.Items.Add(person);

          foreach (var simGroup in similar.OrderByDescending(x => x.sim))
            group.Items.Add(simGroup.person);
        }
      }
      else {
        const string zzzz = "zzzz";
        foreach (var group in tmp
          .GroupBy(x => x.person.Keywords == null
            ? zzzz
            : string.Join(", ", KeywordsM.GetAllKeywords(x.person.Keywords).Select(k => k.Name)))
          .OrderBy(g => g.Key)
          .ThenBy(g => g.First().person.Name)) {

          var itemsGroup = new ItemsGroup();
          var infoItem = group.Key.Equals(zzzz)
            ? new ItemsGroupInfoItem(Res.IconEmpty, string.Empty)
            : new ItemsGroupInfoItem(Res.IconTag, group.Key);
          itemsGroup.Info.Add(infoItem);
          ConfirmedGrouped.Add(itemsGroup);

          foreach (var (person, _, _) in group)
            itemsGroup.Items.Add(person);
        }
      }
    }

    private void ViewMediaItemsWithSegment(SegmentM segmentM) {
      var items = GetMediaItemsWithSegment(segmentM, _core.MainTabsM.Selected == MainTabsItem);
      if (items == null) return;

      _core.MediaViewerM.SetMediaItems(items, segmentM.MediaItem);
      _core.MainWindowM.IsFullScreen = true;
    }

    private void SegmentMatching() {
      var md = new MessageDialog(
        "Segment Matching",
        "Do you want to load all segments, segments with persons \nor one segment from each person?",
        Res.IconQuestion,
        true);

      md.Buttons = new DialogButton[] {
        new("All segments", null, md.SetResult(1), true),
        new("Segments with persons", null, md.SetResult(2)),
        new("One from each", null, md.SetResult(3)) };

      var result = Core.DialogHostShow(md);

      if (result < 1) return;

      MediaItemsForMatching = _core.ThumbnailsGridsM.Current.GetSelectedOrAll();
      _core.MainTabsM.Activate(MainTabsItem);

      LoadSegments(MediaItemsForMatching, result);
    }
  }
}