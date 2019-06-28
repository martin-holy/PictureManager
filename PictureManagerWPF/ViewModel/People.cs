using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PictureManager.ViewModel {
  public sealed class People : BaseCategoryItem {
    public List<Person> AllPeople; 
    private static readonly Mutex Mut = new Mutex();

    public People() : base(Category.People) {
      AllPeople = new List<Person>();
      Title = "People";
      IconName = IconName.PeopleMultiple;
    }

    ~People() {
      Mut.Dispose();
    }

    public void Load() {
      Items.Clear();
      AllPeople.Clear();

      LoadGroups();

      //Add People in Group
      foreach (var g in Items.Cast<CategoryGroup>()) {
        foreach (var person in (from p in ACore.Db.People
                                join cgi in ACore.Db.CategoryGroupsItems 
                                on new { pid = p.Id, gid = g.Data.Id } equals new { pid = cgi.ItemId, gid = cgi.CategoryGroupId }
                                select p).OrderBy(x => x.Name).Select(x => new Person(x) {Parent = g})) {
          g.Items.Add(person);
          AllPeople.Add(person);
        }
      }

      //Add People without Group
      foreach (var person in (from p in ACore.Db.People where AllPeople.All(ap => ap.Data.Id != p.Id) select p)
          .OrderBy(x => x.Name).Select(x => new Person(x) {Parent = this})) {
        Items.Add(person);
        AllPeople.Add(person);
      }
    }

    public Person GetPerson(int id) {
      return AllPeople.SingleOrDefault(x => x.Data.Id == id);
    }

    public Person GetPerson(string name, bool create) {
      return AllPeople.SingleOrDefault(x => x.Title.Equals(name)) ?? (create ? CreatePerson(this, name) : null);
      /*if (create) Mut.WaitOne();
      var person = AllPeople.SingleOrDefault(x => x.Title.Equals(name));
      if (person != null) {
        Mut.ReleaseMutex();
        return person;
      }
      if (!create) return null;
      var newPerson = CreatePerson(this, name);
      Mut.ReleaseMutex();
      return newPerson;*/
    }

    public Person CreatePerson(BaseTreeViewItem root, string name) {
      if (root == null) return null;
      Mut.WaitOne();
      var dmPerson = new DataModel.Person {
        Id = ACore.Db.GetNextIdFor<DataModel.Person>(),
        Name = name
      };

      ACore.Db.Insert(dmPerson);

      InsertCategoryGroupItem(root, dmPerson.Id);

      var vmPerson = new Person(dmPerson) {Parent = root};
      AllPeople.Add(vmPerson);
      ACore.People.ItemSetInPlace(root, true, vmPerson);
      Mut.ReleaseMutex();
      return vmPerson;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      var inputDialog = ItemGetInputDialog(item, IconName.People, "Person", rename);
      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, item.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (ACore.Db.People.SingleOrDefault(x => x.Name.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("This person already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) {
        var person = (Person) item;
        person.Title = inputDialog.Answer;
        ACore.Db.Update(person.Data);
        ACore.People.ItemSetInPlace(person.Parent, false, person);
      } else CreatePerson(item, inputDialog.Answer);
    }

    public override void ItemDelete(BaseTreeViewItem item) {
      if (!(item is Person person)) return;
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
      AllPeople.Remove(person);
    }
  }
}
