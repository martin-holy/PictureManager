using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class People : BaseCategoryItem, ITable {
    public TableHelper Helper { get; set; }
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();
    private static readonly Mutex Mut = new Mutex();

    public People() : base(Category.People) {
      Title = "People";
      IconName = IconName.PeopleMultiple;
    }

    ~People() {
      Mut.Dispose();
    }

    public void NewFromCsv(string csv) {
      // ID|Name
      var props = csv.Split('|');
      if (props.Length != 2) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new Person(id, props[1]));
    }

    public void LinkReferences(SimpleDB sdb) {
      // MediaItems to the Person are added in LinkReferences on MediaItem

      Items.Clear();
      LoadGroups();

      // add People without group
      foreach (var person in Records.Values.Cast<Person>().Where(x => x.Parent == null)) {
        person.Parent = this;
        Items.Add(person);
      }
    }

    public Person GetPerson(int id) {
      if (Records.TryGetValue(id, out var person))
        return (Person) person;
      return null;
    }

    public Person GetPerson(string name, bool create) {
      var person = Records.Values.Cast<Person>().SingleOrDefault(x => x.Title.Equals(name));
      return person ?? (create ? CreatePerson(this, name) : null);
    }

    public Person CreatePerson(BaseTreeViewItem root, string name) {
      Mut.WaitOne();
      var id = ACore.People.Helper.GetNextId();
      var person = new Person(id, name);
      
      // add new Person to the database
      ACore.People.Helper.AddRecord(person);

      // add new Person to the tree
      person.Parent = root;
      root.Items.Add(person);
      
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

        if (ACore.People.Records.Cast<Person>().SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
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
      }
      else CreatePerson(item, inputDialog.Answer);
    }

    public override void ItemDelete(BaseTreeViewItem item) {
      if (!(item is Person person)) return;

      // remove Person from MediaItems
      foreach (var bmi in person.MediaItems)
        bmi.People.Remove(person);

      // remove Person from the tree
      person.Parent.Items.Remove(person);

      // remove Person from DB
      Records.Remove(person.Id);
    }
  }
}
