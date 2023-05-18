using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.HelperClasses;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Dialogs {
  public sealed class SetSegmentPersonDialogM : Dialog {
    private readonly SegmentsM _segmentsM;
    private PersonM _person;

    public PersonM Person {
      get => _person;
      set {
        _person = value;
        OnPropertyChanged();
        Segments = _segmentsM.GetSegmentsToUpdate(_person, People);
        ReloadSegments();
      }
    }

    public ObservableCollection<PersonM> People { get; } = new();
    public SegmentM[] Segments { get; set; }
    public ObservableCollection<object> GroupedSegments { get; } = new();
    public RelayCommand<PersonM> SelectCommand { get; }

    public SetSegmentPersonDialogM(SegmentsM segmentsM, IEnumerable<PersonM> people) : base("Select a person for segments", Res.IconPeople) {
      _segmentsM = segmentsM;
      SelectCommand = new(x => Person = x);
      Buttons = new DialogButton[] {
        new("Ok", Res.IconCheckMark, YesOkCommand, true),
        new("Cancel", Res.IconXCross, CloseCommand, false, true) };

      foreach (var person in people)
        People.Add(person);
    }

    private void ReloadSegments() {
      var groups = Segments
        .GroupBy(x => x.Person)
        .OrderBy(x => x.Key == null ? "?" : x.Key.Name);
      ItemsGroup group;
      GroupedSegments.Clear();

      foreach (var g in groups) {
        group = new();
        group.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, g.Key == null ? "?" : g.Key.Name));
        GroupedSegments.Add(group);

        foreach (var segment in g.OrderBy(x => x.MediaItem.FileName))
          group.Items.Add(segment);
      }
    }
  }
}
