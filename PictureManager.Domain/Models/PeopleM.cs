using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.EventsArgs;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class PeopleM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private readonly Core _core;

    public DataAdapter DataAdapter { get; }
    public List<PersonM> All { get; } = new();
    public Dictionary<int, PersonM> AllDic { get; set; }

    public event EventHandler<PersonDeletedEventArgs> PersonDeletedEvent = delegate { };

    public PeopleM(Core core) {
      _core = core;
      DataAdapter = new PeopleDataAdapter(core, this);
    }

    private static string GetItemName(object item) => item is PersonM p ? p.Name : string.Empty;

    public IEnumerable<PersonM> GetAll() {
      foreach (var cg in Items.OfType<CategoryGroupM>())
        foreach (var personM in cg.Items.Cast<PersonM>())
          yield return personM;

      foreach (var personM in Items.OfType<PersonM>())
        yield return personM;
    }

    public void ToggleKeyword(PersonM person, KeywordM keyword) {
      person.Keywords = ListExtensions.Toggle(person.Keywords, keyword, true);
      DataAdapter.IsModified = true;
    }

    public PersonM ItemCreate(ITreeBranch root, string name) {
      var item = new PersonM(DataAdapter.GetNextId(), name) { Parent = root };
      root.Items.SetInOrder(item, GetItemName);
      All.Add(item);
      DataAdapter.IsModified = true;

      return item;
    }

    public void ItemMove(PersonM item, ITreeLeaf dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest, GetItemName);
      DataAdapter.IsModified = true;
    }

    public bool ItemCanRename(string name) =>
      !All.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void ItemRename(PersonM item, string name) {
      item.Name = name;
      item.Parent.Items.SetInOrder(item, GetItemName);
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(PersonM person) {
      _core.MediaItemsM.RemovePersonFromMediaItems(person);
      _core.Segments.RemovePersonFromSegments(person);
      person.Parent.Items.Remove(person);
      person.Parent = null;
      person.Segment = null;
      person.Segments = null;
      person.Keywords = null;
      All.Remove(person);
      PersonDeletedEvent(this, new(person));
      DataAdapter.IsModified = true;
    }

    public PersonM GetPerson(string name, bool create) =>
      All.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
      (create ? ItemCreate(this, name) : null);

    public void DeleteNotUsed(IEnumerable<PersonM> list) {
      var people = new HashSet<PersonM>(list);
      foreach (var mi in _core.MediaItemsM.All.Cast<MediaItemM>()) {
        if (mi.People != null)
          foreach (var personM in mi.People.Where(x => people.Contains(x)))
            people.Remove(personM);

        if (mi.Segments != null)
          foreach (var segment in mi.Segments.Where(x => people.Contains(x.Person)))
            people.Remove(segment.Person);

        if (people.Count == 0) break;
      }

      foreach (var person in people)
        ItemDelete(person);
    }

    public List<MediaItemM> GetMediaItems(PersonM person) =>
      _core.MediaItemsM.All.Where(mi =>
          mi.People?.Contains(person) == true ||
          mi.Segments?.Any(s => s.Person == person) == true)
        .OrderBy(mi => mi.FileName).ToList();

    public void RemoveKeywordsFromPeople(IEnumerable<KeywordM> keywords) {
      var set = new HashSet<KeywordM>(keywords);
      foreach (var person in All.Where(p => p.Keywords != null)) {
        foreach (var keyword in person.Keywords.Where(set.Contains)) {
          person.Keywords = ListExtensions.Toggle(person.Keywords, keyword, true);
          DataAdapter.IsModified = true;
        }
      }
    }
  }
}
