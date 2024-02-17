﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.TreeCategories;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class PeopleM {
  private readonly PeopleDA _da;

  public PeopleTreeCategory TreeCategory { get; }
  public PeopleView PeopleView { get; private set; }
  public Selecting<PersonM> Selected { get; } = new();

  public PeopleM(PeopleDA da) {
    _da = da;
    TreeCategory = new(this, _da);
  }

  public void OpenPeopleView() {
    PeopleView ??= new();
    PeopleView.Reload();
    Core.MainTabs.Activate(Res.IconPeopleMultiple, "People", PeopleView);
  }

  public void ToggleTopSegment(PersonM person, SegmentM segment) {
    if (segment == null) return;

    person.ToggleTopSegment(segment);

    if (person.TopSegments?.Count > 0)
      person.Segment = person.TopSegments[0];

    _da.IsModified = true;
  }

  public void Select(SelectionEventArgs<PersonM> e) {
    if (!e.IsCtrlOn && !e.IsShiftOn)
      Core.M.Segments.Selected.DeselectAll();

    var segmentsBefore = Selected.Items
      .Where(x => x.Segment != null)
      .Select(x => x.Segment)
      .ToArray();

    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);

    var segmentsAfter = Selected.Items
      .Where(x => x.Segment != null)
      .Select(x => x.Segment)
      .ToArray();

    Core.M.Segments.Selected.Set(segmentsBefore.Except(segmentsAfter), false);
    Core.M.Segments.Selected.Add(segmentsAfter);
    Core.M.Segments.OnPropertyChanged(nameof(Core.M.Segments.CanSetAsSamePerson));
  }

  public void MergePeople(PersonM person, PersonM[] people) {
    if (people.Length == 0) return;

    var topSegments = people
      .Where(x => x.TopSegments != null)
      .SelectMany(x => x.TopSegments)
      .Distinct();

    foreach (var segment in topSegments)
      ToggleTopSegment(person, segment);

    var keywords = people
      .GetKeywords()
      .Except(person.Keywords.EmptyIfNull())
      .ToArray();

    _da.ToggleKeywords(person, keywords);
  }

  public static PersonM[] GetAll() =>
    Core.Db.People.All
      .Where(x => x.Parent is not CategoryGroupM { IsHidden: true })
      .OrderBy(x => x.Name)
      .ToArray();
}