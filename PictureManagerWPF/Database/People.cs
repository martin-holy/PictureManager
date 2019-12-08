using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class People : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
    public List<Person> All { get; } = new List<Person>();
    public Dictionary<int, Person> AllDic { get; } = new Dictionary<int, Person>();

    private static readonly Mutex Mut = new Mutex();

    public People() : base(Category.People) {
      Title = "People";
      IconName = IconName.PeopleMultiple;
    }

    ~People() {
      Mut.Dispose();
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      AllDic.Clear();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|Name
      var props = csv.Split('|');
      if (props.Length != 2) return;
      var id = int.Parse(props[0]);
      AddRecord(new Person(id, props[1]));
    }

    public void LinkReferences() {
      // MediaItems to the Person are added in LinkReferences on MediaItem

      Items.Clear();
      LoadGroups();

      // add People without group
      foreach (var person in All.Where(x => x.Parent == null).OrderBy(x => x.Title)) {
        person.Parent = this;
        Items.Add(person);
      }

      // sort People in the Groups
      foreach (var g in Items.OfType<CategoryGroup>())
        g.Items.Sort(x => x.Title);
    }

    private void AddRecord(Person record) {
      All.Add(record);
      AllDic.Add(record.Id, record);
    }

    public Person GetPerson(string name, bool create) {
      var person = All.SingleOrDefault(x => x.Title.Equals(name));
      return person ?? (create ? CreatePerson(this, name) : null);
    } 

    public Person CreatePerson(BaseTreeViewItem root, string name) {
      Mut.WaitOne();
      var person = new Person(Helper.GetNextId(), name);

      // add new Person to the database
      AddRecord(person);

      // add new Person to the tree
      person.Parent = root;
      ItemSetInPlace(root, true, person);

      if (root is CategoryGroup)
        App.Core.CategoryGroups.Helper.IsModified = true;

      Mut.ReleaseMutex();

      return person;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      var inputDialog = ItemGetInputDialog(item, IconName.People, "Person", rename);
      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, item.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (All.SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("This person already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) {
        var person = (Person)item;
        person.Title = inputDialog.Answer;
        ItemSetInPlace(person.Parent, false, person);
        SaveToFile();
      }
      else {
        CreatePerson(item, inputDialog.Answer);
        App.Core.Sdb.SaveAllTables();
      }
    }

    public override void ItemDelete(BaseTreeViewItem item) {
      if (!(item is Person person)) return;

      // remove Person from MediaItems
      foreach (var mi in person.MediaItems) {
        mi.People.Remove(person);
        if (mi.People.Count == 0)
          mi.People = null;
      }

      // remove Person from the tree
      person.Parent.Items.Remove(person);

      // remove Person from DB
      All.Remove(person);
      AllDic.Remove(person.Id);

      Helper.IsModified = true;
      if (person.MediaItems.Count > 0)
        App.Core.MediaItems.Helper.IsModified = true;
    }
  }
}
