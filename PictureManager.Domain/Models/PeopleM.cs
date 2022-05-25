using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.HelperClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.HelperClasses;

namespace PictureManager.Domain.Models {
  public sealed class PeopleM : TreeCategoryBase {
    private readonly CategoryGroupsM _categoryGroupsM;

    public PeopleDataAdapter DataAdapter { get; set; }
    public List<PersonM> Selected { get; } = new();
    public ObservableCollection<object> PeopleInGroups { get; } = new();

    public event EventHandler<ObjectEventArgs<PersonM>> PersonDeletedEventHandler = delegate { };
    public event EventHandler PeopleKeywordChangedEvent = delegate { };

    public PeopleM(CategoryGroupsM categoryGroupsM) : base(Res.IconPeopleMultiple, Category.People, "People") {
      _categoryGroupsM = categoryGroupsM;
      CanMoveItem = true;
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

    public IEnumerable<PersonM> GetAll() {
      foreach (var cg in Items.OfType<CategoryGroupM>())
        foreach (var personM in cg.Items.Cast<PersonM>())
          yield return personM;

      foreach (var personM in Items.OfType<PersonM>())
        yield return personM;
    }

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

      DataAdapter.IsModified = true;
    }

    private void ToggleKeyword(PersonM person, KeywordM keyword) {
      person.Keywords = ListExtensions.Toggle(person.Keywords, keyword, true);
      DataAdapter.IsModified = true;
    }

    public void RemoveKeywordFromPeople(KeywordM keyword) {
      foreach (var person in DataAdapter.All.Values.Where(x => x.Keywords?.Contains(keyword) == true))
        ToggleKeyword(person, keyword);

      PeopleKeywordChangedEvent(this, EventArgs.Empty);
    }

    public void ToggleKeywordOnSelected(KeywordM keyword) {
      foreach (var person in Selected) {
        ToggleKeyword(person, keyword);
        person.UpdateDisplayKeywords();
      }

      PeopleKeywordChangedEvent(this, EventArgs.Empty);
    }

    public void Select(List<PersonM> list, PersonM p, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select(Selected, list, p, isCtrlOn, isShiftOn, null);

    public void DeselectAll() =>
      Selecting.DeselectAll(Selected, null);

    public void SetSelected(PersonM p, bool value) =>
      Selecting.SetSelected(Selected, p, value, null);

    public void ReloadPeopleInGroups() {
      PeopleInGroups.Clear();

      // add people in groups
      foreach (var group in Items.OfType<CategoryGroupM>().Where(x => !x.IsHidden))
        AddPeopleToGroups(group.Name, group.Items.Cast<PersonM>());

      // add people without group
      var peopleWithoutGroup = Items.OfType<PersonM>().ToArray();
      if (peopleWithoutGroup.Any())
        AddPeopleToGroups(string.Empty, peopleWithoutGroup);
    }

    private void AddPeopleToGroups(string groupTitle, IEnumerable<PersonM> people) {
      // group people by keywords
      foreach (var group in people
                 .GroupBy(p => p.DisplayKeywords == null
                   ? string.Empty
                   : string.Join(", ", p.DisplayKeywords.Select(dk => dk.Name)))
                 .OrderBy(g => g.Key)) {

        var itemsGroup = new ItemsGroup();
        if (!string.IsNullOrEmpty(groupTitle))
          itemsGroup.Info.Add(new ItemsGroupInfoItem(Res.IconPeople, groupTitle));
        if (!group.Key.Equals(string.Empty))
          itemsGroup.Info.Add(new ItemsGroupInfoItem(Res.IconTag, group.Key));

        PeopleInGroups.Add(itemsGroup);

        foreach (var person in group.OrderBy(p => p.Name))
          itemsGroup.Items.Add(person);

        itemsGroup.Info.Add(new ItemsGroupInfoItem(Res.IconImageMultiple, itemsGroup.Items.Count.ToString()));
      }
    }
  }
}
