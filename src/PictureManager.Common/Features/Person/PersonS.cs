﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Extensions;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Interfaces;
using System.Linq;

namespace PictureManager.Common.Features.Person;

public sealed class PersonS(PersonR r) : ObservableObject {
  public Selecting<PersonM> Selected { get; } = new();

  public void ToggleTopSegment(PersonM person, SegmentM? segment) {
    if (segment == null) return;

    person.ToggleTopSegment(segment);

    if (person.TopSegments?.Count > 0)
      person.Segment = person.TopSegments[0];

    r.IsModified = true;
  }

  public void Select(SelectionEventArgs<PersonM> e) {
    if (!e.IsCtrlOn && !e.IsShiftOn)
      Core.S.Segment.Selected.DeselectAll();

    var segmentsBefore = Selected.Items
      .Where(x => x.Segment != null)
      .Select(x => x.Segment!)
      .ToArray();

    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);

    var segmentsAfter = Selected.Items
      .Where(x => x.Segment != null)
      .Select(x => x.Segment!)
      .ToArray();

    Core.S.Segment.Selected.Set(segmentsBefore.Except(segmentsAfter), false);
    Core.S.Segment.Selected.Add(segmentsAfter);
    Core.S.Segment.OnPropertyChanged(nameof(Core.S.Segment.CanSetAsSamePerson));
  }

  public void MergePeople(PersonM person, PersonM[] people) {
    if (people.Length == 0) return;

    var topSegments = people
      .Where(x => x.TopSegments != null)
      .SelectMany(x => x.TopSegments!)
      .Distinct();

    foreach (var segment in topSegments)
      ToggleTopSegment(person, segment);

    var keywords = people
      .GetKeywords()
      .Except(person.Keywords.EmptyIfNull())
      .ToArray();

    r.ToggleKeywords(person, keywords);
  }
}