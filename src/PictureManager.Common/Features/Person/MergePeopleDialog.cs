using MH.UI.Controls;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Segment;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Person;

public sealed class MergePeopleDialog : Dialog {
  private static MergePeopleDialog? _inst;
  private readonly SegmentS _segmentS;
  private PersonM[] _people = null!;
  private SegmentM[] _unknownSelected = null!;
  private SegmentM[] _segmentsToUpdate = null!;
  private PersonM _person = null!;

  public PersonM Person { get => _person; private set { _person = value; OnPropertyChanged(); } }
  public PersonCollectionView PeopleView { get; }
  public SegmentCollectionView SegmentsView { get; }

  public MergePeopleDialog(SegmentS segmentS) : base("Select a person for segments", Res.IconPeople) {
    _segmentS = segmentS;
    PeopleView = new() { CanOpen = false, IsMultiSelect = false };
    SegmentsView = new() { CanSelect = false, CanOpen = false };
    Buttons = [
      new(OkCommand, true),
      new(CloseCommand, false, true)
    ];
  }

  public static bool Open(PersonS personS, SegmentS segmentS, PersonM[] people, out PersonM? person, out SegmentM[]? segmentsToUpdate) {
    if (_inst == null) {
      _inst = new(segmentS);
      _inst.PeopleView.ItemSelectedEvent += (_, e) =>
        _inst._setPerson(e.Item);
    }

    return _inst._open(personS, segmentS, people, out person, out segmentsToUpdate);
  }

  private bool _open(PersonS personS, SegmentS segmentS, PersonM[] people, out PersonM? person, out SegmentM[]? segmentsToUpdate) {
    _people = people;
    _unknownSelected = segmentS.Selected.Items.Where(x => x.Person == null).ToArray();
    personS.Selected.Select(people[0]);
    _setPerson(people[0]);
    PeopleView.Reload(people.ToList(), GroupMode.ThenByRecursive, null, true);

    if (Show(this) != 1) {
      _clear();
      person = null;
      segmentsToUpdate = null;
      return false;
    }

    personS.MergePeople(Person, people.Where(x => !ReferenceEquals(x, Person)).ToArray());
    _clear();
    person = Person;
    segmentsToUpdate = _segmentsToUpdate;

    return true;
  }

  private void _setPerson(PersonM person) {
    Person = person;
    _segmentsToUpdate = _getSegmentsToUpdate(Person, _people);
    var source = _segmentsToUpdate.OrderBy(x => x.MediaItem.FileName).ToList();
    var groupByItems = GroupByItems.GetPeople(_segmentsToUpdate).ToArray();

    SegmentsView.Reload(source, GroupMode.GroupBy, groupByItems, true, "Segments to update");
  }

  private SegmentM[] _getSegmentsToUpdate(PersonM person, IEnumerable<PersonM> people) {
    var oldPeople = people.Where(x => !ReferenceEquals(x, person)).ToHashSet();
    return _segmentS.DataAdapter.All
      .Where(x => x.Person != null && oldPeople.Contains(x.Person))
      .Concat(_unknownSelected)
      .ToArray();
  }

  private void _clear() {
    PeopleView.Root.Clear();
    SegmentsView.Root.Clear();
  }
}