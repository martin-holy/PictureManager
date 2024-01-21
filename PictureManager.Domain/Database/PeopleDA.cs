using MH.Utils;
using MH.Utils.BaseClasses;
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
public class PeopleDA : TreeDataAdapter<PersonM> {
  private readonly Db _db;
  private const string _unknownPersonNamePrefix = "P -";
  private const string _notFoundRecordNamePrefix = "Not found ";

  public PeopleM Model { get; }
  public event DataEventHandler<PersonM[]> KeywordsChangedEvent = delegate { };

  public PeopleDA(Db db) : base("People", 4) {
    _db = db;
    Model = new(this);
  }

  public IEnumerable<PersonM> GetAll() {
    foreach (var cg in Model.TreeCategory.Items.OfType<CategoryGroupM>())
      foreach (var personM in cg.Items.Cast<PersonM>())
        yield return personM;

    foreach (var personM in Model.TreeCategory.Items.OfType<PersonM>())
      yield return personM;
  }

  public override void Save() =>
    SaveToSingleFile(GetAll());

  public override PersonM FromCsv(string[] csv) {
    var person = new PersonM(int.Parse(csv[0]), csv[1]);

    if (person.Name.StartsWith(_unknownPersonNamePrefix))
      person.IsUnknown = true;

    return person;
  }

  public override string ToCsv(PersonM person) =>
    string.Join("|",
      person.GetHashCode().ToString(),
      person.Name,
      person.TopSegments.ToHashCodes().ToCsv(),
      person.Keywords.ToHashCodes().ToCsv());

  public override void LinkReferences() {
    _db.CategoryGroups.LinkGroups(Model.TreeCategory, AllDict);

    foreach (var (person, csv) in AllCsv) {
      // Persons top segments
      var ts = _db.Segments.Link(csv[2], this);
      if (ts != null) person.TopSegments = new(ts);
      if (person.TopSegments != null)
        person.Segment = person.TopSegments[0];

      // reference to Keywords
      person.Keywords = _db.Keywords.Link(csv[3], this);

      // add loose people
      foreach (var personM in AllDict.Values.Where(x => x.Parent == null)) {
        personM.Parent = Model.TreeCategory;
        personM.Parent.Items.Add(personM);
      }
    }
  }

  public List<PersonM> Link(string csv, IDataAdapter seeker) =>
    LinkList(csv, GetNotFoundRecord, seeker);

  public PersonM GetPerson(int id, IDataAdapter seeker) =>
    AllDict.TryGetValue(id, out var person)
      ? person
      : ResolveNotFoundRecord(id, GetNotFoundRecord, seeker);

  private PersonM GetNotFoundRecord(int notFoundId) {
    var id = GetNextId();
    var item = new PersonM(id, $"{_notFoundRecordNamePrefix}{id} ({notFoundId})") {
      Parent = Model.TreeCategory
    };
    item.Parent.Items.Add(item);
    IsModified = true;
    return item;
  }

  public override PersonM ItemCreate(ITreeItem parent, string name) =>
    TreeItemCreate(new(GetNextId(), name) { Parent = parent });

  public PersonM ItemCreateUnknown() {
    var id = SimpleDB.GetNextRecycledId(All.Select(x => x.Id).ToHashSet()) ?? GetNextId();

    return TreeItemCreate(new(id, $"{_unknownPersonNamePrefix}{id}") {
      Parent = Model.TreeCategory.UnknownGroup,
      IsUnknown = true
    });
  }

  protected override void OnItemRenamed(PersonM item) {
    if (item.IsUnknown) item.IsUnknown = false;
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
    if (newPerson != null) newPerson.Segment ??= segment;
    if (oldPerson == null) return;
    if (ReferenceEquals(oldPerson.Segment, segment)) oldPerson.Segment = null;
    if (oldPerson.TopSegments?.Contains(segment) != true) return;
    oldPerson.ToggleTopSegment(segment);
    IsModified = true;
  }

  public void OnSegmentsPersonChanged(PersonM person, SegmentM[] segments, PersonM[] people) {
    // delete unknown people without segments
    var toDelete = person == null
      ? people
      : people.Where(x => !ReferenceEquals(x, person)).ToArray();

    // WARNING Segments.All contains only segments from available drives!
    // When the drive is mounted, not found people will be recreated.
    foreach (var ptd in toDelete)
      if (ptd.IsUnknown && !_db.Segments.All.Any(x => ReferenceEquals(x.Person, ptd)))
        ItemDelete(ptd);
  }

  public void RemoveKeyword(KeywordM keyword) =>
    ToggleKeyword(All.Where(x => x.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(PersonM[] people, KeywordM keyword) =>
    keyword.Toggle(people, _ => IsModified = true, () => KeywordsChangedEvent(people));

  public void ToggleKeywords(PersonM person, IEnumerable<KeywordM> keywords) {
    foreach (var keyword in keywords)
      person.Keywords = person.Keywords.Toggle(keyword);

    IsModified = true;
    KeywordsChangedEvent(new[] { person });
  }

  public void MoveGroupItemsToRoot(CategoryGroupM group) {
    if (group.Category != Category.People) return;
    foreach (var item in group.Items.ToArray())
      ItemMove(item, Model.TreeCategory, false);
  }
}