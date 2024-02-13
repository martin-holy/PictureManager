using MH.Utils;
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
  public PeopleToolsTabM PeopleToolsTabM { get; private set; }
  public PersonDetail PersonDetail { get; private set; }
  public Selecting<PersonM> Selected { get; } = new();
  public RelayCommand OpenPeopleToolsTabCommand { get; }
  public RelayCommand<PersonM> OpenPersonDetailCommand { get; }

  public PeopleM(PeopleDA da) {
    _da = da;
    TreeCategory = new(this, _da);
    OpenPeopleToolsTabCommand = new(OpenPeopleToolsTab, Res.IconPeopleMultiple, "People");
    OpenPersonDetailCommand = new(OpenPersonDetail, Res.IconInformation, "Detail");
  }

  private void OpenPeopleToolsTab() {
    PeopleToolsTabM ??= new();
    PeopleToolsTabM.ReloadFrom();
    Core.VM.MainWindow.ToolsTabs.Activate(Res.IconPeopleMultiple, "People", PeopleToolsTabM);
  }

  public void OpenPeopleView() {
    PeopleView ??= new();
    PeopleView.Reload();
    Core.MainTabs.Activate(Res.IconPeopleMultiple, "People", PeopleView);
  }

  public void OpenPersonDetail(PersonM person) {
    PersonDetail ??= new(this, Core.SegmentsM);
    PersonDetail.Reload(person);
    Core.VM.MainWindow.ToolsTabs.Activate(Res.IconPeople, "Person", PersonDetail);
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
      Core.SegmentsM.Selected.DeselectAll();

    var segmentsBefore = Selected.Items
      .Where(x => x.Segment != null)
      .Select(x => x.Segment)
      .ToArray();

    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);

    var segmentsAfter = Selected.Items
      .Where(x => x.Segment != null)
      .Select(x => x.Segment)
      .ToArray();

    Core.SegmentsM.Selected.Set(segmentsBefore.Except(segmentsAfter), false);
    Core.SegmentsM.Selected.Add(segmentsAfter);
    Core.SegmentsM.OnPropertyChanged(nameof(Core.SegmentsM.CanSetAsSamePerson));
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