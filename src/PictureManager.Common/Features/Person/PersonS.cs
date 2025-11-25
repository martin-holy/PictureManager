using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Extensions;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Person;

public sealed class PersonS(PersonR r) : ObservableObject {
  public Selecting<PersonM> Selected { get; } = new();

  public void ToggleTopSegment(PersonM person, SegmentM? segment) {
    if (segment == null) return;
    person.ToggleTopSegment(segment);
    _updatePersonSegment(person);
    r.IsModified = true;
  }

  public void AddToTopSegments(PersonM person, IEnumerable<SegmentM> segments) {
    var toAdd = segments.Where(x => ReferenceEquals(person, x.Person) && (person.TopSegments?.Contains(x) != true)).ToArray();
    if (toAdd.Length == 0) return;

    if (person.TopSegments == null) {
      person.TopSegments = new(toAdd);
      person.OnPropertyChanged(nameof(person.TopSegments));
    }
    else
      person.TopSegments.AddItems(toAdd, null);

    _updatePersonSegment(person);
    r.IsModified = true;
  }

  public void RemoveFromTopSegments(PersonM person, IEnumerable<SegmentM> segments) {
    if (person.TopSegments == null) return;
    var toRemove = segments.Where(person.TopSegments.Contains).ToArray();
    if (toRemove.Length == 0) return;

    person.TopSegments.RemoveItems(toRemove, null);

    if (person.TopSegments.Count == 0) {
      person.TopSegments = null;
      person.OnPropertyChanged(nameof(person.TopSegments));
    }

    _updatePersonSegment(person);
    r.IsModified = true;
  }

  private static void _updatePersonSegment(PersonM person) {
    if (person.TopSegments?.Count > 0)
      person.Segment = person.TopSegments[0];
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