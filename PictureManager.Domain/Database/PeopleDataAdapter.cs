using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|Segments|Keywords
/// </summary>
public class PeopleDataAdapter : TreeDataAdapter<PersonM> {
  private readonly Db _db;

  public PeopleM Model { get; }
  public event EventHandler<ObjectEventArgs<PersonM[]>> PeopleKeywordsChangedEvent = delegate { };

  public PeopleDataAdapter(Db db) : base("People", 4) {
    _db = db;
    _db.ReadyEvent += OnDbReady;
    Model = new(this);
  }

  private void OnDbReady(object sender, EventArgs args) {
    // move all group items to root
    _db.CategoryGroups.ItemDeletedEvent += (_, e) => {
      if (e.Data.Category != Category.People) return;
      foreach (var item in e.Data.Items.ToArray())
        ItemMove(item, Model.TreeCategory, false);
    };
  }

  public IEnumerable<PersonM> GetAll() {
    foreach (var cg in Model.TreeCategory.Items.OfType<CategoryGroupM>())
      foreach (var personM in cg.Items.Cast<PersonM>())
        yield return personM;

    foreach (var personM in Model.TreeCategory.Items.OfType<PersonM>())
      yield return personM;
  }

  public override void Save() =>
    SaveToFile(GetAll());

  public override PersonM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(PersonM person) =>
    string.Join("|",
      person.GetHashCode().ToString(),
      person.Name,
      person.TopSegments == null
        ? string.Empty
        : string.Join(",", person.TopSegments.Select(x => x.GetHashCode().ToString())),
      person.Keywords == null
        ? string.Empty
        : string.Join(",", person.Keywords.Select(x => x.GetHashCode().ToString())));

  public override void LinkReferences() {
    _db.CategoryGroups.LinkGroups(Model.TreeCategory, AllDict);

    foreach (var (person, csv) in AllCsv) {
      // Persons top segments
      person.TopSegments = LinkObservableCollection(csv[2], _db.Segments.AllDict);
      if (person.TopSegments != null)
        person.Segment = (SegmentM)person.TopSegments[0];

      // reference to Keywords
      person.Keywords = LinkList(csv[3], _db.Keywords.AllDict);

      // add loose people
      foreach (var personM in AllDict.Values.Where(x => x.Parent == null && x.Id > 0)) {
        personM.Parent = Model.TreeCategory;
        Model.TreeCategory.Items.Add(personM);
      }
    }
  }

  public override PersonM ItemCreate(ITreeItem parent, string name) =>
    TreeItemCreate(new(GetNextId(), name) { Parent = parent });

  public PersonM ItemCreateUnknown() {
    var id = -1;
    var usedIds = All
      .Where(x => x.Id < 0)
      .Select(x => x.Id)
      .OrderByDescending(x => x)
      .ToArray();

    if (usedIds.Any())
      for (var i = -1; i > usedIds.Min() - 2; i--) {
        if (usedIds.Contains(i)) continue;
        id = i;
        break;
      }

    return TreeItemCreate(new(id, $"P {id}") { Parent = Model.TreeCategory.UnknownGroup });
  }

  public override void ItemRename(ITreeItem item, string name) {
    if (((PersonM)item).Id > 0)
      base.ItemRename(item, name);
    else
      Dialog.Show(new MessageDialog(
        "Person rename",
        "Renaming unknown person is not supported yet.",
        Res.IconPeople,
        false));
  }

  protected override void OnItemDeleted(PersonM item) {
    item.Parent?.Items.Remove(item);
    item.Parent = null;
    item.Segment = null;
    item.TopSegments = null;
    item.Keywords = null;
  }

  public PersonM GetPerson(string name, bool create) =>
    All.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
    ?? (create ? ItemCreate(Model.TreeCategory, name) : null);

  public void OnSegmentPersonChanged(SegmentM segment, PersonM oldPerson, PersonM newPerson) {
    if (newPerson != null)
      newPerson.Segment ??= segment;

    if (oldPerson == null) return;
    
    if (oldPerson.Segment == segment)
      oldPerson.Segment = null;

    if (oldPerson.TopSegments?.Contains(segment) != true) return;

    oldPerson.TopSegments = ObservableCollectionExtensions.Toggle(oldPerson.TopSegments, segment, true);
    IsModified = true;
  }

  public void OnSegmentsPersonChanged(PersonM person, SegmentM[] segments, PersonM[] people) {
    // delete unknown people without segments
    var toDelete = person == null
      ? people
      : people.Where(x => !ReferenceEquals(x, person)).ToArray();

    foreach (var ptd in toDelete)
      if (ptd.Id < 0 && !Core.Db.Segments.All.Any(x => ReferenceEquals(x.Person, ptd)))
        ItemDelete(ptd);
  }

  public void RemoveKeywordFromPeople(KeywordM keyword) =>
    ToggleKeyword(All.Where(x => x.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(PersonM[] people, KeywordM keyword) {
    foreach (var person in people) {
      person.Keywords = KeywordsM.Toggle(person.Keywords, keyword);
      IsModified = true;
    }

    PeopleKeywordsChangedEvent(this, new(people));
  }

  public void ToggleKeywords(PersonM person, IEnumerable<KeywordM> keywords) {
    foreach (var keyword in keywords)
      person.Keywords = KeywordsM.Toggle(person.Keywords, keyword);

    IsModified = true;
    PeopleKeywordsChangedEvent(this, new(new[] { person }));
  }
}