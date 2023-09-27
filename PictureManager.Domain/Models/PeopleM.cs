using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class PeopleM {
  public PeopleTreeCategory TreeCategory { get; }
  public PeopleView PeopleView { get; private set; }
  public PeopleToolsTabM PeopleToolsTabM { get; private set; }
  public PersonDetail PersonDetail { get; private set; }
  public Selecting<PersonM> Selected { get; } = new();
  public RelayCommand<object> OpenPeopleToolsTabCommand { get; }
  public RelayCommand<PersonM> OpenPersonDetailCommand { get; }

  public event EventHandler<ObjectEventArgs<PersonM[]>> PeopleKeywordChangedEvent = delegate { };

  public PeopleM() {
    TreeCategory = new(this);
    OpenPeopleToolsTabCommand = new(OpenPeopleToolsTab);
    OpenPersonDetailCommand = new(OpenPersonDetail);
  }

  private void OpenPeopleToolsTab() {
    PeopleToolsTabM ??= new(this);
    PeopleToolsTabM.ReloadFrom();
    Core.ToolsTabsM.Activate(Res.IconPeopleMultiple, "People", PeopleToolsTabM);
    Core.ToolsTabsM.Open();
  }

  public void OpenPeopleView() {
    PeopleView ??= new(this);
    PeopleView.Reload();
    Core.MainTabs.Activate(Res.IconPeopleMultiple, "People", PeopleView);
  }

  public void OpenPersonDetail(PersonM person) {
    PersonDetail ??= new(this, Core.SegmentsM);
    PersonDetail.Reload(person);
    Core.ToolsTabsM.Activate(Res.IconPeople, "Person", PersonDetail);
    Core.ToolsTabsM.Open();
  }

  public void OnSegmentPersonChanged(SegmentM segment, PersonM oldPerson, PersonM newPerson) {
    if (newPerson != null)
      newPerson.Segment ??= segment;

    if (oldPerson == null) return;

    if (oldPerson.Segment == segment)
      oldPerson.Segment = null;

    if (oldPerson.TopSegments?.Contains(segment) != true) return;

    oldPerson.TopSegments = ObservableCollectionExtensions.Toggle(oldPerson.TopSegments, segment, true);
    Core.Db.People.IsModified = true;
  }

  public void ToggleTopSegment(PersonM person, SegmentM segment) {
    if (segment == null) return;

    var collectionIsNull = person.TopSegments == null;
    person.TopSegments = ObservableCollectionExtensions.Toggle(person.TopSegments, segment, true);

    if (collectionIsNull)
      person.OnPropertyChanged(nameof(person.TopSegments));

    if (person.TopSegments?.Count > 0)
      person.Segment = (SegmentM)person.TopSegments[0];

    Core.Db.People.IsModified = true;
  }

  private void ToggleKeyword(PersonM person, KeywordM keyword) {
    person.Keywords = ListExtensions.Toggle(person.Keywords, keyword, true);
    person.UpdateDisplayKeywords();
    Core.Db.People.IsModified = true;
  }

  private void ToggleKeyword(IEnumerable<PersonM> people, KeywordM keyword) {
    var p = people.ToArray();
    foreach (var person in p)
      ToggleKeyword(person, keyword);

    PeopleKeywordChangedEvent(this, new(p));
  }

  public void RemoveKeywordFromPeople(KeywordM keyword) =>
    ToggleKeyword(Core.Db.People.All
      .Where(x => x.Keywords?.Contains(keyword) == true), keyword);

  public void ToggleKeywordOnSelected(KeywordM keyword) =>
    ToggleKeyword(Selected.Items, keyword);

  public void Select(List<PersonM> people, PersonM person, bool isCtrlOn, bool isShiftOn) {
    if (!isCtrlOn && !isShiftOn)
      Core.SegmentsM.Selected.DeselectAll();

    var segmentsBefore = Selected.Items
      .Where(x => x.Segment != null)
      .Select(x => x.Segment)
      .ToArray();

    Selected.Select(people, person, isCtrlOn, isShiftOn);

    var segmentsAfter = Selected.Items
      .Where(x => x.Segment != null)
      .Select(x => x.Segment)
      .ToArray();

    Core.SegmentsM.Selected.Set(segmentsBefore.Except(segmentsAfter), false);
    Core.SegmentsM.Selected.Add(segmentsAfter);
    Core.SegmentsM.SetCanSelectAsSamePerson();
  }

  /// <summary>
  /// Update Person TopSegments and Keywords from People
  /// and then remove not used People from DB
  /// </summary>
  /// <param name="person"></param>
  /// <param name="people"></param>
  public void MergePeople(PersonM person, PersonM[] people) {
    if (people.Length == 0) return;

    var topSegments = people
      .Where(x => x.TopSegments != null)
      .SelectMany(x => x.TopSegments)
      .Distinct()
      .ToArray();

    var keywords = people
      .Where(x => x.Keywords != null)
      .SelectMany(x => x.Keywords)
      .Distinct()
      .Except(person.Keywords ?? Enumerable.Empty<KeywordM>())
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

    if (PersonDetail != null && people.Contains(PersonDetail?.PersonM))
      PersonDetail?.Reload(person);

    foreach (var oldPerson in people)
      Core.Db.People.All.Remove(oldPerson);

    Core.Db.People.RaisePeopleDeleted(new(people));
  }

  public static PersonM[] GetFromMediaItems(MediaItemM[] mediaItems) =>
    mediaItems == null
      ? Array.Empty<PersonM>()
      : mediaItems
        .Where(x => x.People != null)
        .SelectMany(x => x.People)
        .Concat(mediaItems
          .Where(y => y.Segments != null)
          .SelectMany(y => y.Segments)
          .Where(y => y.Person != null)
          .Select(y => y.Person))
        .Distinct()
        .ToArray();

  public static PersonM[] GetFromSegments(SegmentM[] segments) =>
    segments == null
      ? Array.Empty<PersonM>()
      : segments
        .Where(x => x.Person != null)
        .Select(x => x.Person)
        .Distinct()
        .ToArray();

  public static PersonM[] GetAll() =>
    Core.Db.People.All
      .Where(x => x.Parent is not CategoryGroupM { IsHidden: true })
      .OrderBy(x => x.Name)
      .ToArray();
}