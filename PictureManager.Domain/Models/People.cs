using System.Collections.Generic;
using System.Linq;
using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class People : BaseCatTreeViewCategory, ITable, ICatTreeViewCategory {
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new List<IRecord>();
    public Dictionary<int, Person> AllDic { get; set; }

    public People() : base(Category.People) {
      Title = "People";
      IconName = IconName.PeopleMultiple;
      CanHaveGroups = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      AllDic = new Dictionary<int, Person>();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|Name
      var props = csv.Split('|');
      if (props.Length != 2) return;
      var id = int.Parse(props[0]);
      var person = new Person(id, props[1]);
      All.Add(person);
      AllDic.Add(person.Id, person);
    }

    public void LinkReferences() {
      // MediaItems to the Person are added in LinkReferences on MediaItem

      Items.Clear();
      LoadGroupsAndItems(All);
    }

    public Person GetPerson(string name, bool create) {
      return Core.Instance.RunOnUiThread(() => {
        var person = All.Cast<Person>().SingleOrDefault(x => x.Title.Equals(name));
        return person ?? (create ? ItemCreate(this, name) as Person : null);
      }).Result;
    }

    public new ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var item = new Person(Helper.GetNextId(), name) {Parent = root};
      var idx = CatTreeViewUtils.SetItemInPlace(root, item);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, root, idx);
      if (allIdx < 0) All.Add(item); else All.Insert(allIdx, item);

      SaveToFile();
      if (root is ICatTreeViewGroup)
        Core.Instance.CategoryGroups.SaveToFile();

      Core.Instance.Sdb.SaveIdSequences();

      return item;
    }

    public new void ItemDelete(ICatTreeViewItem item) {
      if (!(item is Person person)) return;

      // remove Person from MediaItems
      if (person.MediaItems.Count > 0) {
        foreach (var mi in person.MediaItems) {
          mi.People.Remove(person);
          if (mi.People.Count == 0)
            mi.People = null;
        }
        Core.Instance.MediaItems.SaveToFile();
      }

      // remove Person from the tree
      item.Parent.Items.Remove(item);
      if (item.Parent is CategoryGroup)
        Core.Instance.CategoryGroups.SaveToFile();
      item.Parent = null;

      // remove Person from DB
      All.Remove(person);

      SaveToFile();
    }
  }
}
