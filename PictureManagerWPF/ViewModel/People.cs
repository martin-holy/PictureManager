using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class People : BaseCategoryItem {
    public List<Person> AllPeople; 
    private static readonly Mutex Mut = new Mutex();

    public People() : base(Categories.People) {
      AllPeople = new List<Person>();
      Title = "People";
      IconName = "appbar_people_multiple";
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
                                on new { pid = p.Id, gid = g.Id } equals new { pid = cgi.ItemId, gid = cgi.CategoryGroupId }
                                select p).OrderBy(x => x.Name).Select(x => new Person(x) {Parent = g})) {
          g.Items.Add(person);
          AllPeople.Add(person);
        }
      }

      //Add People without Group
      foreach (var person in (from p in ACore.Db.People where AllPeople.All(ap => ap.Id != p.Id) select p)
          .OrderBy(x => x.Name).Select(x => new Person(x) {Parent = this})) {
        Items.Add(person);
        AllPeople.Add(person);
      }
    }

    public Person GetPerson(int id) {
      return AllPeople.SingleOrDefault(x => x.Id == id);
    }

    public Person GetPerson(string name, bool create) {
      if (create) Mut.WaitOne();
      var dmPerson = ACore.Db.People.SingleOrDefault(x => x.Name.Equals(name));
      if (dmPerson != null) {
        Mut.ReleaseMutex();
        return GetPerson(dmPerson.Id);
      }
      if (!create) return null;
      var newPerson = CreatePerson(this, name);
      Mut.ReleaseMutex();
      return newPerson;
    }

    public Person CreatePerson(BaseTreeViewItem root, string name) {
      if (root == null) return null;

      var dmPerson = new DataModel.Person {
        Id = ACore.Db.GetNextIdFor<DataModel.Person>(),
        Name = name
      };

      ACore.Db.Insert(dmPerson);

      InsertCategoryGroupItem(root, dmPerson.Id);

      var vmPerson = new Person(dmPerson) {Parent = root};
      AllPeople.Add(vmPerson);
      ACore.People.ItemSetInPlace(root, true, vmPerson);
      return vmPerson;
    }

    public override void ItemNewOrRename(BaseTreeViewItem item, bool rename) {
      InputDialog inputDialog = ItemGetInputDialog(item, "appbar_people", "Person", rename);

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          var person = (Person)item;
          person.Title = inputDialog.Answer;
          person.Data.Name = inputDialog.Answer;
          ACore.Db.Update(person.Data);
          ACore.People.ItemSetInPlace(person.Parent, false, person);
        } else CreatePerson(item, inputDialog.Answer);
      }
    }

    public override void ItemDelete(BaseTreeViewTagItem item) {
      //TODO: SubmitChanges can submit other not commited changes as well!!
      var person = item as Person;
      if (person == null) return;

      foreach (var mip in ACore.Db.MediaItemPeople.Where(x => x.PersonId == person.Id)) {
        ACore.Db.DeleteOnSubmit(mip);
      }

      var cgi = ACore.Db.CategoryGroupsItems.SingleOrDefault(
            x => x.ItemId == item.Id && x.CategoryGroupId == (item.Parent as CategoryGroup)?.Id);
      if (cgi != null) {
        ACore.Db.DeleteOnSubmit(cgi);
      }

      ACore.Db.DeleteOnSubmit(person.Data);
      ACore.Db.SubmitChanges();

      item.Parent.Items.Remove(person);
      AllPeople.Remove(person);
    }
  }
}
