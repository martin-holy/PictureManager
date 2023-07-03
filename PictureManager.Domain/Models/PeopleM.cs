using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using PictureManager.Domain.ViewModels;

namespace PictureManager.Domain.Models {
  public sealed class PeopleM : TreeCategoryBase {
    private readonly Core _core;
    private readonly CategoryGroupsM _categoryGroupsM;

    public HeaderedListItem<object, string> MainTabsItem { get; set; }
    public PeopleDataAdapter DataAdapter { get; set; }
    public PeopleVM View { get; }
    public PeopleToolsTabM PeopleToolsTabM { get; set; }
    public Selecting<PersonM> Selected { get; } = new();
    public RelayCommand<object> OpenPeopleToolsTabCommand { get; }

    public event EventHandler<ObjectEventArgs<PersonM>> PersonDeletedEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<PersonM[]>> PeopleDeletedEvent = delegate { };
    public event EventHandler<ObjectEventArgs<PersonM[]>> PeopleKeywordChangedEvent = delegate { };

    public PeopleM(Core core, CategoryGroupsM categoryGroupsM) : base(Res.IconPeopleMultiple, Category.People, "People") {
      _core = core;
      _categoryGroupsM = categoryGroupsM;
      CanMoveItem = true;
      MainTabsItem = new(this, "People");
      View = new(this);

      OpenPeopleToolsTabCommand = new(OpenPeopleToolsTab);
    }

    private void OpenPeopleToolsTab() {
      PeopleToolsTabM ??= new(_core, this);
      _core.ToolsTabsM.Activate(PeopleToolsTabM.ToolsTabsItem, true);
    }

    protected override ITreeItem ModelItemCreate(ITreeItem root, string name) {
      var item = new PersonM(DataAdapter.GetNextId(), name) { Parent = root };
      Tree.SetInOrder(root.Items, item, x => x.Name);
      DataAdapter.All.Add(item);

      return item;
    }

    protected override void ModelItemRename(ITreeItem item, string name) {
      item.Name = name;
      Tree.SetInOrder(item.Parent.Items, item, x => x.Name);
      DataAdapter.IsModified = true;
    }

    protected override void ModelItemDelete(ITreeItem item) {
      var person = (PersonM)item;
      person.Parent.Items.Remove(person);
      person.Parent = null;
      person.Segment = null;
      person.TopSegments = null;
      person.Keywords = null;
      DataAdapter.All.Remove(person);
      PersonDeletedEventHandler(this, new(person));
      PeopleDeletedEvent(this, new(new[] { person }));
      DataAdapter.IsModified = true;
    }

    public override void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      DataAdapter.All.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ? $"{name} item already exists!"
        : null;

    protected override void ModelGroupCreate(ITreeItem root, string name) =>
      _categoryGroupsM.GroupCreate(name, Category);

    protected override void ModelGroupRename(ITreeGroup group, string name) =>
      _categoryGroupsM.GroupRename(group, name);

    protected override void ModelGroupDelete(ITreeGroup group) =>
      _categoryGroupsM.GroupDelete(group);

    public override void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) =>
      _categoryGroupsM.GroupMove(group, dest, aboveDest);

    protected override string ValidateNewGroupName(ITreeItem root, string name) =>
      CategoryGroupsM.ItemCanRename(root, name)
        ? null
        : $"{name} group already exists!";

    public PersonM GetPerson(string name, bool create) =>
      DataAdapter.All.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
      ?? (create
        ? (PersonM)ModelItemCreate(this, name)
        : null);

    public void SegmentPersonChange(SegmentM segment, PersonM oldPerson, PersonM newPerson) {
      if (newPerson != null)
        newPerson.Segment ??= segment;

      if (oldPerson == null) return;

      if (oldPerson.Segment == segment)
        oldPerson.Segment = null;

      if (oldPerson.TopSegments?.Contains(segment) != true) return;

      oldPerson.TopSegments = ObservableCollectionExtensions.Toggle(oldPerson.TopSegments, segment, true);
      DataAdapter.IsModified = true;
    }

    public void ToggleTopSegment(PersonM person, SegmentM segment) {
      if (segment == null) return;

      var collectionIsNull = person.TopSegments == null;
      person.TopSegments = ObservableCollectionExtensions.Toggle(person.TopSegments, segment, true);

      if (collectionIsNull)
        person.OnPropertyChanged(nameof(person.TopSegments));

      if (person.TopSegments?.Count > 0)
        person.Segment = (SegmentM)person.TopSegments[0];

      DataAdapter.IsModified = true;
    }

    private void ToggleKeyword(PersonM person, KeywordM keyword) {
      person.Keywords = ListExtensions.Toggle(person.Keywords, keyword, true);
      person.UpdateDisplayKeywords();
      DataAdapter.IsModified = true;
    }

    private void ToggleKeyword(IEnumerable<PersonM> people, KeywordM keyword) {
      foreach (var person in people)
        ToggleKeyword(person, keyword);

      PeopleKeywordChangedEvent(this, new(people.ToArray()));
    }

    public void RemoveKeywordFromPeople(KeywordM keyword) =>
      ToggleKeyword(DataAdapter.All
        .Where(x => x.Keywords?.Contains(keyword) == true), keyword);

    public void ToggleKeywordOnSelected(KeywordM keyword) =>
      ToggleKeyword(Selected.Items, keyword);

    public void Select(List<PersonM> people, PersonM person, bool isCtrlOn, bool isShiftOn) {
      if (!isCtrlOn && !isShiftOn)
        _core.SegmentsM.Selected.DeselectAll();

      var segmentsBefore = Selected.Items
        .Where(x => x.Segment != null)
        .Select(x => x.Segment)
        .ToArray();

      Selected.Select(people, person, isCtrlOn, isShiftOn);

      var segmentsAfter = Selected.Items
        .Where(x => x.Segment != null)
        .Select(x => x.Segment)
        .ToArray();

      _core.SegmentsM.Selected.Set(segmentsBefore.Except(segmentsAfter), false);
      _core.SegmentsM.Selected.Add(segmentsAfter);
      _core.SegmentsM.SetCanSelectAsSamePerson();
    }

    /// <summary>
    /// Update Person TopSegments and Keywords from People
    /// and then remove not used People from DB
    /// </summary>
    /// <param name="person"></param>
    /// <param name="people"></param>
    public void MergePeople(PersonM person, PersonM[] people) {
      var topSegments = people
        .Where(x => x.TopSegments != null)
        .SelectMany(x => x.TopSegments)
        .Distinct()
        .ToArray();
      var keywords = people
        .Where(x => x.Keywords != null)
        .SelectMany(x => x.Keywords)
        .Distinct()
        .ToArray();

      if (topSegments.Any()) {
        if (person.TopSegments == null) {
          person.TopSegments = new();
          person.OnPropertyChanged(nameof(person.TopSegments));
        }
        
        foreach (var segment in topSegments)
          person.TopSegments.Add(segment);

        person.Segment = (SegmentM)person.TopSegments[0];
      }

      if (keywords.Any()) {
        person.Keywords ??= new();
        foreach (var keyword in keywords)
          person.Keywords.Add(keyword);

        person.UpdateDisplayKeywords();
      }

      if (people.Contains(_core.PersonDetailM.PersonM))
        _core.PersonDetailM.PersonM = person;

      foreach (var oldPerson in people)
        DataAdapter.All.Remove(oldPerson);

      PeopleDeletedEvent(this, new(people));
    }

    public static PersonM[] GetFromMediaItems(MediaItemM[] mediaItems) =>
      mediaItems == null
        ? Array.Empty<PersonM>()
        : mediaItems
            .Where(x => x.People != null)
            .SelectMany(x => x.People)
            .Concat(mediaItems
              .Where(y => y.Segments != null)
              .SelectMany(y => y.Segments)
              .Where(y => y.Person != null)
              .Select(y => y.Person))
            .Distinct()
            .ToArray();
  }
}
