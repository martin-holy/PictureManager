using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.TreeCategories;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class PeopleM {
  private readonly PeopleDataAdapter _da;

  public PeopleTreeCategory TreeCategory { get; }
  public PeopleView PeopleView { get; private set; }
  public PeopleToolsTabM PeopleToolsTabM { get; private set; }
  public PersonDetail PersonDetail { get; private set; }
  public Selecting<PersonM> Selected { get; } = new();
  public RelayCommand<object> OpenPeopleToolsTabCommand { get; }
  public RelayCommand<PersonM> OpenPersonDetailCommand { get; }

  public PeopleM(PeopleDataAdapter da) {
    _da = da;
    _da.ItemDeletedEvent += OnItemDeleted;
    _da.PeopleKeywordsChangedEvent += OnPeopleKeywordsChanged;
    TreeCategory = new(this, _da);
    OpenPeopleToolsTabCommand = new(OpenPeopleToolsTab);
    OpenPersonDetailCommand = new(OpenPersonDetail);
  }

  private void OnItemDeleted(object sender, ObjectEventArgs<PersonM> e) {
    Selected.Set(e.Data, false);
    PeopleView?.ReGroupItems(new[] { e.Data }, true);
    PeopleToolsTabM?.ReGroupItems(new[] { e.Data }, true);

    if (ReferenceEquals(PersonDetail?.PersonM, e.Data))
      Core.ToolsTabsM.Close(PersonDetail);
  }

  private void OnPeopleKeywordsChanged(object sender, ObjectEventArgs<PersonM[]> e) {
    PeopleToolsTabM?.ReGroupItems(e.Data, false);
    PeopleView?.ReGroupItems(e.Data, false);

    foreach (var person in e.Data)
      person.UpdateDisplayKeywords();
  }

  private void OpenPeopleToolsTab() {
    if (PeopleToolsTabM == null) {
      PeopleToolsTabM = new();
      AddEvents(PeopleToolsTabM);
    }

    PeopleToolsTabM.ReloadFrom();
    Core.ToolsTabsM.Activate(Res.IconPeopleMultiple, "People", PeopleToolsTabM);
    Core.ToolsTabsM.Open();
  }

  public void OpenPeopleView() {
    if (PeopleView == null) {
      PeopleView = new();
      AddEvents(PeopleView);
    }

    PeopleView.Reload();
    Core.MainTabs.Activate(Res.IconPeopleMultiple, "People", PeopleView);
  }

  public void OpenPersonDetail(PersonM person) {
    if (PersonDetail == null) {
      PersonDetail = new(this, Core.SegmentsM);
      Core.SegmentsM.AddEvents(PersonDetail.TopSegments);
      Core.SegmentsM.AddEvents(PersonDetail.AllSegments);
    }

    PersonDetail.Reload(person);
    Core.ToolsTabsM.Activate(Res.IconPeople, "Person", PersonDetail);
    Core.ToolsTabsM.Open();
  }

  public void ToggleTopSegment(PersonM person, SegmentM segment) {
    if (segment == null) return;

    var collectionIsNull = person.TopSegments == null;
    person.TopSegments = ObservableCollectionExtensions.Toggle(person.TopSegments, segment, true);

    if (collectionIsNull)
      person.OnPropertyChanged(nameof(person.TopSegments));

    if (person.TopSegments?.Count > 0)
      person.Segment = (SegmentM)person.TopSegments[0];

    _da.IsModified = true;
  }

  public void AddEvents(CollectionViewPeople cv) {
    cv.ItemOpenedEvent += (_, e) => Open(cv, e);
    cv.ItemSelectedEvent += (_, e) => Select(e);
  }

  public void Open(object o, ObjectEventArgs<PersonM> e) =>
    Core.SegmentsM.ViewMediaItemsWithSegment(o, e.Data.Segment);

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
    Core.SegmentsM.SetCanSetAsSamePerson();
  }

  public void MergePeople(PersonM person, PersonM[] people) {
    if (people.Length == 0) return;

    var topSegments = people
      .Where(x => x.TopSegments != null)
      .SelectMany(x => x.TopSegments)
      .Distinct()
      .Cast<SegmentM>();

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