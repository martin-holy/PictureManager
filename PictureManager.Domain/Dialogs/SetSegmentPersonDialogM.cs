using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Dialogs;

public sealed class SetSegmentPersonDialogM : Dialog {
  private readonly SegmentsM _segmentsM;
  private PersonM _person;

  public PersonM Person {
    get => _person;
    set {
      _person = value;
      OnPropertyChanged();
      Segments = GetSegmentsToUpdate(_person, People);
      ReloadSegments();
    }
  }

  public ObservableCollection<PersonM> People { get; } = new();
  public SegmentM[] Segments { get; set; }
  public CollectionViewSegments GroupedSegments { get; }
  public RelayCommand<PersonM> SelectCommand { get; }

  public SetSegmentPersonDialogM(SegmentsM segmentsM, IEnumerable<PersonM> people) : base("Select a person for segments", Res.IconPeople) {
    _segmentsM = segmentsM;
    GroupedSegments = new(segmentsM) { SelectionDisabled = true };
    SelectCommand = new(x => Person = x);
    Buttons = new DialogButton[] {
      new("Ok", Res.IconCheckMark, YesOkCommand, true),
      new("Cancel", Res.IconXCross, CloseCommand, false, true) };

    foreach (var person in people)
      People.Add(person);

    Person = People[0];
  }

  private void ReloadSegments() {
    var source = Segments.OrderBy(x => x.MediaItem.FileName).ToList();
    var groupByItems = GroupByItems.GetPeopleFromSegments(Segments).ToArray();

    GroupedSegments.Reload(source, GroupMode.GroupBy, groupByItems, true, "Segments to update");
  }

  private SegmentM[] GetSegmentsToUpdate(PersonM person, IEnumerable<PersonM> people) {
    var oldPeople = people.Where(x => !x.Equals(person)).ToHashSet();
    return Core.Db.Segments.All
      .Where(x => oldPeople.Contains(x.Person))
      .Concat(_segmentsM.Selected.Items.Where(x => x.Person == null))
      .ToArray();
  }
}