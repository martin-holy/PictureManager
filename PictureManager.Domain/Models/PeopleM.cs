using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;

namespace PictureManager.Domain.Models {
  public sealed class PeopleM : TreeCategoryBase {
    private readonly CategoryGroupsM _categoryGroupsM;
    private TreeWrapGroup _peopleRoot;
    private object _scrollToItem;

    public HeaderedListItem<object, string> MainTabsItem { get; set; }
    public PeopleDataAdapter DataAdapter { get; set; }
    public List<PersonM> Selected { get; } = new();
    public TreeWrapGroup PeopleRoot { get => _peopleRoot; private set { _peopleRoot = value; OnPropertyChanged(); } }
    public object ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    
    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }

    public event EventHandler<ObjectEventArgs<PersonM>> PersonDeletedEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<PersonM>> PersonTopSegmentsChangedEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<PersonM[]>> PeopleKeywordChangedEvent = delegate { };

    public PeopleM(Core core, CategoryGroupsM categoryGroupsM) : base(Res.IconPeopleMultiple, Category.People, "People") {
      _categoryGroupsM = categoryGroupsM;
      CanMoveItem = true;
      MainTabsItem = new(this, "People");

      SelectCommand = new(Select);
    }

    protected override ITreeItem ModelItemCreate(ITreeItem root, string name) {
      var item = new PersonM(DataAdapter.GetNextId(), name) { Parent = root };
      Tree.SetInOrder(root.Items, item, x => x.Name);
      DataAdapter.All.Add(item.Id, item);

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
      DataAdapter.All.Remove(person.Id);
      PersonDeletedEventHandler(this, new(person));
      DataAdapter.IsModified = true;
    }

    public override void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      DataAdapter.All.Values.Any(x => x.Name.Equals(name, StringComparison.CurrentCulture))
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
      DataAdapter.All.Values.SingleOrDefault(x => x.Name.Equals(name, StringComparison.CurrentCulture))
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

      PersonTopSegmentsChangedEventHandler(this, new(person));

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
      ToggleKeyword(DataAdapter.All.Values
        .Where(x => x.Keywords?.Contains(keyword) == true), keyword);

    public void ToggleKeywordOnSelected(KeywordM keyword) =>
      ToggleKeyword(Selected, keyword);

    private void Select(MouseButtonEventArgs e) {
      if (e.IsSourceDesired && e.DataContext is SegmentM segmentM)
        Select(null, segmentM.Person, e.IsCtrlOn, e.IsShiftOn);
    }

    public void Select(List<PersonM> list, PersonM p, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select(Selected, list, p, isCtrlOn, isShiftOn, null);

    public void DeselectAll() =>
      Selecting.DeselectAll(Selected, null);

    public void SetSelected(PersonM p, bool value) =>
      Selecting.SetSelected(Selected, p, value, null);

    public void Reload() {
      var root = new TreeWrapGroup();

      // add people in groups
      foreach (var group in Items.OfType<CategoryGroupM>().Where(x => !x.IsHidden))
        AddPeopleToGroups(root, group.Name, group.Items.Cast<PersonM>());

      // add people without group
      var peopleWithoutGroup = Items.OfType<PersonM>().ToArray();
      if (peopleWithoutGroup.Any())
        AddPeopleToGroups(root, string.Empty, peopleWithoutGroup);

      PeopleRoot = root;
      ScrollToItem = (PeopleRoot?.Items.FirstOrDefault() as TreeWrapGroup)?.Items.FirstOrDefault();

      // TODO do it just for loaded
      foreach (var person in DataAdapter.All.Values)
        person.UpdateDisplayKeywords();
    }

    private static void AddPeopleToGroups(TreeWrapGroup root, string groupTitle, IEnumerable<PersonM> people) {
      var pGroup = new TreeWrapGroup();
      root.Items.Add(pGroup);
      pGroup.Info.Add(new(Res.IconPeople, groupTitle));

      var groupedByKeywords = people
            .GroupBy(x => x.DisplayKeywords == null
              ? string.Empty
              : string.Join(", ", x.DisplayKeywords.Select(dk => dk.Name)))
            .OrderBy(x => x.Key)
            .ToArray();

      switch (groupedByKeywords.Length) {
        case 0:
          return;
        case 1: {
          foreach (var person in groupedByKeywords[0].OrderBy(x => x.Name))
            pGroup.Items.Add(person);

          pGroup.Info.Add(new(Res.IconImageMultiple, pGroup.Items.Count.ToString()));

          return;
        }
      }

      var count = 0;
      foreach (var group in groupedByKeywords) {
        var kGroup = new TreeWrapGroup();
        pGroup.Items.Add(kGroup);

        if (!group.Key.Equals(string.Empty))
          kGroup.Info.Add(new(Res.IconTag, group.Key));

        foreach (var person in group.OrderBy(p => p.Name))
          kGroup.Items.Add(person);

        kGroup.Info.Add(new(Res.IconImageMultiple, kGroup.Items.Count.ToString()));
        count += kGroup.Items.Count;
      }

      pGroup.Info.Add(new(Res.IconImageMultiple, count.ToString()));
    }
  }
}
