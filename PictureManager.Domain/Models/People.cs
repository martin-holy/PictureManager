using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Utils;
using SimpleDB;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class People : BaseCatTreeViewCategory, ITable {
    private readonly Core _core;
    private List<Person> _selected = new();
    private Person _current;

    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, Person> AllDic { get; set; }
    public List<Person> Selected => _selected;
    public Person Current { get => _current; set { _current = value; OnPropertyChanged(); } }

    public People(Core core) : base(Category.People) {
      _core = core;
      DataAdapter = new PeopleDataAdapter(core, this);
      Title = "People";
      IconName = IconName.PeopleMultiple;
      CanHaveGroups = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
    }

    public Person GetPerson(string name, bool create) =>
      _core.RunOnUiThread(() => {
        var person = All.Cast<Person>().SingleOrDefault(x => x.Title.Equals(name));
        return person ?? (create ? ItemCreate(this, name) as Person : null);
      }).Result;

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var item = new Person(DataAdapter.GetNextId(), name) { Parent = root };
      var idx = CatTreeViewUtils.SetItemInPlace(root, item);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, root, idx);

      All.Insert(allIdx, item);
      DataAdapter.IsModified = true;
      if (root is ICatTreeViewGroup)
        _core.CategoryGroups.DataAdapter.IsModified = true;

      return item;
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      if (item is not Person person) return;

      // remove Person from MediaItems
      if (person.MediaItems.Count > 0) {
        foreach (var mi in person.MediaItems) {
          mi.People.Remove(person);
          if (mi.People.Count == 0)
            mi.People = null;
        }
        _core.MediaItems.DataAdapter.IsModified = true;
      }

      // remove Person from the tree
      item.Parent.Items.Remove(item);
      if (item.Parent is ICatTreeViewGroup)
        _core.CategoryGroups.DataAdapter.IsModified = true;
      item.Parent = null;

      // set Person Segments to unknown
      if (person.Segments != null) {
        foreach (var segment in person.Segments) {
          segment.PersonId = 0;
          segment.Person = null;
          _core.Segments.DataAdapter.IsModified = true;
        }
        person.Segments = null;
      }

      person.Keywords?.Clear();

      // remove Person from DB
      All.Remove(person);

      DataAdapter.IsModified = true;
    }

    public void Select(List<Person> list, Person p, bool isCtrlOn, bool isShiftOn) =>
      Selecting.Select(ref _selected, list, p, isCtrlOn, isShiftOn, null);

    public void DeselectAll() => Selecting.DeselectAll(ref _selected, null);

    public void SetSelected(Person p, bool value) => Selecting.SetSelected(ref _selected, p, value, null);

    /// <summary>
    /// Toggle Person on Media Item
    /// </summary>
    /// <param name="p">Person</param>
    /// <param name="mi">Media Item</param>
    public static void Toggle(Person p, MediaItem mi) {
      if (p.IsMarked) {
        mi.People ??= new();
        mi.People.Add(p);
        p.MediaItems.Add(mi);
      }
      else {
        mi.People?.Remove(p);
        p.MediaItems.Remove(mi);
        if (mi.People?.Count == 0)
          mi.People = null;
      }
    }

    public void ToggleKeywordOnSelected(Keyword keyword) {
      foreach (var person in Selected) {
        ToggleKeyword(person, keyword);
        person.UpdateDisplayKeywords();
      }
    }

    public static void ToggleKeyword(Person person, Keyword keyword) {
      var currentKeywords = person.Keywords;
      Keywords.Toggle(keyword, ref currentKeywords, null, null);
      person.Keywords = currentKeywords;
      Core.Instance.People.DataAdapter.IsModified = true;
    }
  }
}
