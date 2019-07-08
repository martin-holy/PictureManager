using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VM = PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class People : VM.BaseCategoryItem, ITable {
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
      var props = csv.Split('|');
      if (props.Length != 2) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new Person(id, props[1]) {Csv = props});
    }

    public void LinkReferences(SimpleDB sdb) {
      // nothing yet, later every person will have list of MediaItems
      foreach (var item in Records) {
        var person = (Person)item.Value;

        // csv array is not needed any more
        person.Csv = null;
      }
    }

    public void Load() {
      //TODO
      /*Items.Clear();
      AllPeople.Clear();

      LoadGroups();

      //Add People in Group
      foreach (var g in Items.Cast<CategoryGroup>()) {
        foreach (var person in (from p in ACore.Db.People
                                join cgi in ACore.Db.CategoryGroupsItems
                                on new { pid = p.Id, gid = g.Data.Id } equals new { pid = cgi.ItemId, gid = cgi.CategoryGroupId }
                                select p).OrderBy(x => x.Name).Select(x => new Person(x) { Parent = g })) {
          g.Items.Add(person);
          AllPeople.Add(person);
        }
      }

      //Add People without Group
      foreach (var person in (from p in ACore.Db.People where AllPeople.All(ap => ap.Data.Id != p.Id) select p)
          .OrderBy(x => x.Name).Select(x => new Person(x) { Parent = this })) {
        Items.Add(person);
        AllPeople.Add(person);
      }*/
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

    public Person CreatePerson(VM.BaseTreeViewItem root, string name) {
      // TODO person by mel mit asi parenta
      Mut.WaitOne();
      var id = ACore.Sdb.Table<People>().GetNextId();
      var person = new Person(id, name);
      
      // add new Person to the database
      ACore.Sdb.Table<People>().AddRecord(person);

      // add new Person to the tree
      if (root is CategoryGroup cg)
        cg.Items.Add(person);
      else
        Items.Add(person);
      
      Mut.ReleaseMutex();

      return person;
    }

    public override void ItemNewOrRename(VM.BaseTreeViewItem item, bool rename) {
      var inputDialog = ItemGetInputDialog(item, IconName.People, "Person", rename);
      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, item.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (ACore.Sdb.Table<People>().Table.Records.Cast<Person>().SingleOrDefault(x => x.Title.Equals(inputDialog.Answer)) != null) {
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

    public override void ItemDelete(VM.BaseTreeViewItem item) {
      // TODO person bude mit list of MediaItems a Parenta na skupinu, takze pak bude jednoduchy smazat tyhle vazby
      /*if (!(item is Person person)) return;
      var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();

      foreach (var mip in ACore.Db.MediaItemPeople.Where(x => x.PersonId == person.Data.Id)) {
        DataModel.PmDataContext.DeleteOnSubmit(mip, lists);
      }

      var cgi = ACore.Db.CategoryGroupsItems.SingleOrDefault(
            x => x.ItemId == person.Data.Id && x.CategoryGroupId == (item.Parent as CategoryGroup)?.Data.Id);
      if (cgi != null) {
        DataModel.PmDataContext.DeleteOnSubmit(cgi, lists);
      }

      DataModel.PmDataContext.DeleteOnSubmit(person.Data, lists);
      ACore.Db.SubmitChanges(lists);

      item.Parent.Items.Remove(person);
      AllPeople.Remove(person);*/
    }
  }
}
