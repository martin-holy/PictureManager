using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Dialogs;

public sealed class MergePeopleDialogM : Dialog {
  private static MergePeopleDialogM _inst;
  private readonly PeopleM _peopleM;
  private readonly SegmentsM _segmentsM;
  private static PersonM[] _people;
  private static SegmentM[] _unknownSelected;

  public static PersonM Person { get; private set; }
  public static SegmentM[] SegmentsToUpdate { get; private set; }
  public CollectionViewPeople PeopleView { get; }
  public CollectionViewSegments SegmentsView { get; }

  public MergePeopleDialogM(PeopleM peopleM, SegmentsM segmentsM) : base("Select a person for segments", Res.IconPeople) {
    _peopleM = peopleM;
    _segmentsM = segmentsM;
    PeopleView = new() { CanOpen = false, IsMultiSelect = false };
    SegmentsView = new() { CanSelect = false, CanOpen = false };
    Buttons = new DialogButton[] {
      new("Ok", Res.IconCheckMark, YesOkCommand, true),
      new("Cancel", Res.IconXCross, CloseCommand, false, true) };
  }

  public void SetPerson(SelectionEventArgs<PersonM> e) {
    _peopleM.Select(e);
    Person = e.Item;
    SegmentsToUpdate = GetSegmentsToUpdate(Person, _people);
    var source = SegmentsToUpdate.OrderBy(x => x.MediaItem.FileName).ToList();
    var groupByItems = GroupByItems.GetPeople(SegmentsToUpdate).ToArray();

    SegmentsView.Reload(source, GroupMode.GroupBy, groupByItems, true, "Segments to update");
  }

  private SegmentM[] GetSegmentsToUpdate(PersonM person, IEnumerable<PersonM> people) {
    var oldPeople = people.Where(x => !x.Equals(person)).ToHashSet();
    return _segmentsM.DataAdapter.All
      .Where(x => oldPeople.Contains(x.Person))
      .Concat(_unknownSelected)
      .ToArray();
  }

  private void Clear() {
    PeopleView.Root?.Clear();
    SegmentsView.Root?.Clear();
  }

  public static bool Open(PeopleM peopleM, SegmentsM segmentsM, PersonM[] people) {
    if (_inst == null) {
      _inst = new(peopleM, segmentsM);
      _inst.PeopleView.ItemSelectedEvent += (_, e) =>
        _inst.SetPerson(e);
    }

    _people = people;
    _unknownSelected = segmentsM.Selected.Items.Where(x => x.Person == null).ToArray();
    _inst.SetPerson(new(null, people[0], false, false));
    _inst.PeopleView.Reload(people.ToList(), GroupMode.ThenByRecursive, null, true);

    if (Show(_inst) != 1) {
      _inst.Clear();
      return false;
    }

    peopleM.MergePeople(Person, people.Where(x => !x.Equals(Person)).ToArray());
    _inst.Clear();
    return true;
  }
}