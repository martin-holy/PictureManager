using MH.UI.Controls;
using PictureManager.Common.CollectionViews;
using PictureManager.Common.Models;
using PictureManager.Common.Services;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Dialogs;

public sealed class MergePeopleDialogM : Dialog {
  private static MergePeopleDialogM? _inst;
  private readonly SegmentS _segmentS;
  private static PersonM[] _people = null!;
  private static SegmentM[] _unknownSelected = null!;

  public static PersonM Person { get; private set; } = null!;
  public static SegmentM[] SegmentsToUpdate { get; private set; } = null!;
  public CollectionViewPeople PeopleView { get; }
  public CollectionViewSegments SegmentsView { get; }

  public MergePeopleDialogM(SegmentS segmentS) : base("Select a person for segments", Res.IconPeople) {
    _segmentS = segmentS;
    PeopleView = new() { CanOpen = false, IsMultiSelect = false };
    SegmentsView = new() { CanSelect = false, CanOpen = false };
    Buttons = [
      new(OkCommand, true),
      new(CloseCommand, false, true)
    ];
  }

  public void SetPerson(PersonM person) {
    Person = person;
    SegmentsToUpdate = GetSegmentsToUpdate(Person, _people);
    var source = SegmentsToUpdate.OrderBy(x => x.MediaItem.FileName).ToList();
    var groupByItems = GroupByItems.GetPeople(SegmentsToUpdate).ToArray();

    SegmentsView.Reload(source, GroupMode.GroupBy, groupByItems, true, "Segments to update");
  }

  private SegmentM[] GetSegmentsToUpdate(PersonM person, IEnumerable<PersonM> people) {
    var oldPeople = people.Where(x => !ReferenceEquals(x, person)).ToHashSet();
    return _segmentS.DataAdapter.All
      .Where(x => x.Person != null && oldPeople.Contains(x.Person))
      .Concat(_unknownSelected)
      .ToArray();
  }

  private void Clear() {
    PeopleView.Root?.Clear();
    SegmentsView.Root?.Clear();
  }

  public static bool Open(PersonS personS, SegmentS segmentS, PersonM[] people) {
    if (_inst == null) {
      _inst = new(segmentS);
      _inst.PeopleView.ItemSelectedEvent += (_, e) =>
        _inst.SetPerson(e.Item);
    }

    _people = people;
    _unknownSelected = segmentS.Selected.Items.Where(x => x.Person == null).ToArray();
    personS.Selected.Select(people[0]);
    _inst.SetPerson(people[0]);
    _inst.PeopleView.Reload(people.ToList(), GroupMode.ThenByRecursive, null, true);

    if (Show(_inst) != 1) {
      _inst.Clear();
      return false;
    }

    personS.MergePeople(Person, people.Where(x => !ReferenceEquals(x, Person)).ToArray());
    _inst.Clear();
    return true;
  }
}