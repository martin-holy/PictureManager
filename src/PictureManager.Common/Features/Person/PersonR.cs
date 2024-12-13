using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.CategoryGroup;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Person;

/// <summary>
/// DB fields: ID|Name|Segments|Keywords
/// </summary>
public class PersonR : TreeDataAdapter<PersonM> {
  private readonly CoreR _coreR;
  private const string _unknownPersonNamePrefix = "P -";
  private const string _notFoundRecordNamePrefix = "Not found ";
  private readonly Dictionary<PersonM, List<int>> _notAvailableTopSegments = [];

  public PersonTreeCategory Tree { get; }
  public event EventHandler<PersonM[]> PersonsKeywordsChangedEvent = delegate { };

  public PersonR(CoreR coreR, CategoryGroupR cgR) : base(coreR, "People", 4) {
    _coreR = coreR;
    Tree = new(this, cgR);
  }

  public IEnumerable<PersonM> GetAll() {
    foreach (var cg in Tree.Items.OfType<CategoryGroupM>())
      foreach (var personM in cg.Items.Cast<PersonM>())
        yield return personM;

    foreach (var personM in Tree.Items.OfType<PersonM>())
      yield return personM;
  }

  public IEnumerable<PersonM> GetBy(KeywordM keyword, bool recursive) =>
    All.GetBy(keyword, recursive);

  public override void Save() =>
    _saveToSingleFile(GetAll());

  protected override PersonM _fromCsv(string[] csv) {
    var person = new PersonM(int.Parse(csv[0]), csv[1]);

    if (person.Name.StartsWith(_unknownPersonNamePrefix))
      person.IsUnknown = true;

    return person;
  }

  protected override string _toCsv(PersonM person) =>
    string.Join("|",
      person.GetHashCode().ToString(),
      person.Name,
      TopSegmentsToCsv(person),
      person.Keywords.ToHashCodes().ToCsv());

  public override void LinkReferences() {
    _coreR.CategoryGroup.LinkGroups(Tree, AllDict);

    foreach (var (person, csv) in _allCsv) {
      // top segments
      if (IdsToRecords(csv[2], _coreR.Segment.AllDict) is { } ts) {
        if (ts.Item1.Count > 0) {
          person.TopSegments = new(ts.Item1);
          person.Segment = person.TopSegments[0];
        }

        if (ts.Item2.Count > 0)
          _notAvailableTopSegments.Add(person, ts.Item2);
      }

      // reference to Keywords
      person.Keywords = _coreR.Keyword.Link(csv[3], this);

      // add loose people
      foreach (var personM in AllDict.Values.Where(x => x.Parent == null)) {
        personM.Parent = Tree;
        personM.Parent.Items.Add(personM);
      }
    }
  }

  public List<PersonM>? Link(string csv, IDataAdapter seeker) =>
    LinkList(csv, GetNotFoundRecord, seeker);

  public PersonM GetPerson(int id, IDataAdapter seeker) =>
    AllDict.TryGetValue(id, out var person)
      ? person
      : _resolveNotFoundRecord(id, GetNotFoundRecord, seeker)!;

  // the sort order for not available will be lost so take available first
  private string TopSegmentsToCsv(PersonM person) =>
    _notAvailableTopSegments.TryGetValue(person, out var ts)
      ? (person.TopSegments.ToHashCodes() ?? Array.Empty<int>()).Concat(ts).ToCsv()
      : person.TopSegments.ToHashCodes().ToCsv();

  private PersonM GetNotFoundRecord(int notFoundId) {
    var id = GetNextId();
    var item = new PersonM(id, $"{_notFoundRecordNamePrefix}{id} ({notFoundId})") {
      Parent = Tree
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
      Parent = Tree.UnknownGroup,
      IsUnknown = true
    });
  }

  protected override void _onItemRenamed(PersonM item) {
    if (item.IsUnknown) item.IsUnknown = false;
  }

  protected override void _onItemDeleted(object sender, PersonM item) {
    item.Parent?.Items.Remove(item);
    item.Parent = null;
    item.Segment = null;
    item.TopSegments = null;
    item.Keywords = null;
  }

  public PersonM? GetPerson(string name, bool create) =>
    All.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
    ?? (create ? ItemCreate(Tree, name) : null);

  public void OnSegmentPersonChanged(SegmentM segment, PersonM? oldPerson, PersonM? newPerson) {
    if (newPerson != null) {
      newPerson.Segment ??= segment;
      newPerson.Segments = newPerson.Segments.Toggle(segment, true);
    }

    if (oldPerson == null) return;
    oldPerson.Segments = oldPerson.Segments.Toggle(segment, true);

    if (oldPerson.TopSegments?.Contains(segment) == true) {
      oldPerson.ToggleTopSegment(segment);
      IsModified = true;
    }

    if (ReferenceEquals(oldPerson.Segment, segment))
      oldPerson.Segment = oldPerson.TopSegments?.FirstOrDefault()
                          ?? oldPerson.Segments?.FirstOrDefault();
  }

  public void OnSegmentsPersonChanged(SegmentM[] segments, PersonM? person, PersonM[] people) {
    // delete unknown people without segments
    var toDelete = person == null
      ? people
      : people.Where(x => !ReferenceEquals(x, person)).ToArray();

    // WARNING Segments.All contains only segments from available drives!
    // When the drive is mounted, not found people will be recreated.
    foreach (var ptd in toDelete)
      if (ptd.IsUnknown && !_coreR.Segment.All.Any(x => ReferenceEquals(x.Person, ptd)))
        ItemDelete(ptd);
  }

  public void RemoveKeyword(KeywordM keyword) =>
    ToggleKeyword(All.Where(x => x.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(PersonM[] people, KeywordM keyword) =>
    keyword.Toggle(people, _ => IsModified = true, () => PersonsKeywordsChangedEvent(this, people));

  public void ToggleKeywords(PersonM person, IEnumerable<KeywordM> keywords) {
    foreach (var keyword in keywords)
      person.Keywords = person.Keywords.Toggle(keyword);

    IsModified = true;
    PersonsKeywordsChangedEvent(this, [person]);
  }

  public void MoveGroupItemsToRoot(CategoryGroupM group) {
    if (group.Category != Category.People) return;
    foreach (var item in group.Items.ToArray())
      ItemMove(item, Tree, false);
  }
}