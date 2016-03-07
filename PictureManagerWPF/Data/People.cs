using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using PictureManager.Dialogs;

namespace PictureManager.Data {
  public class People : BaseItem {
    public ObservableCollection<BaseItem> Items { get; set; }
    public List<Person> PeopleList;
    public DbStuff Db;

    public People() {
      Items = new ObservableCollection<BaseItem>();
      PeopleList = new List<Person>();
    }

    public void Load() {
      Items.Clear();
      //PeopleGroups
      const string sqlGroups = "select Id, Name from PeopleGroups order by Name";
      foreach (DataRow rowG in Db.Select(sqlGroups)) {
        var group = new PeopleGroup {
          Id = (int) (long) rowG[0],
          Title = (string) rowG[1],
          IconName = "appbar_people_multiple"
        };
        foreach (DataRow rowP in Db.Select($"select Id, Name from People where PeopleGroupId = {group.Id} order by Name")) {
          var person = new Person {
            Id = (int) (long) rowP[0],
            Title = (string) rowP[1],
            PeopleGroupId = group.Id,
            IconName = "appbar_people"
          };
          group.Items.Add(person);
          PeopleList.Add(person);
        }
        Items.Add(group);
      }

      //People without group
      const string sql = "select Id, Name from People where PeopleGroupId is null order by Name";
      foreach (DataRow row in Db.Select(sql)) {
        var person = new Person {
          Id = (int) (long) row[0],
          Title = (string) row[1],
          PeopleGroupId = -1,
          IconName = "appbar_people"
        };
        Items.Add(person);
        PeopleList.Add(person);
      }
    }

    public Person GetPersonByName(string name, bool create) {
      Person p = PeopleList.FirstOrDefault(x => x.Title.Equals(name));
      if (p != null) return p;
      return create ? CreatePerson(name) : null;
    }

    public Person GetPersonById(int id) {
      return PeopleList.FirstOrDefault(x => x.Id == id);
    }

    public void NewOrRename(WMain wMain, Person person, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = wMain,
        IconName = "appbar_people",
        Title = rename ? "Rename Person" : "New Person",
        Question = rename ? "Enter new name for person." : "Enter name of new person.",
        Answer = rename ? person.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) person.Rename(Db, inputDialog.Answer);
        else CreatePerson(inputDialog.Answer);
      }
    }

    public void NewOrRenameGroup(WMain wMain, PeopleGroup peopleGroup, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = wMain,
        IconName = "appbar_people_multiple",
        Title = rename ? "Rename Group" : "New Group",
        Question = rename ? "Enter new name for group." : "Enter name of new group.",
        Answer = rename ? peopleGroup.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) peopleGroup.Rename(Db, inputDialog.Answer);
        else CreateGroup(inputDialog.Answer);
      }
    }

    public Person CreatePerson(string name) {
      if (!Db.Execute($"insert into People (Name) values ('{name}')")) return null;
      var id = Db.GetLastIdFor("People");
      if (id == null) return null;
      Person newPerson = new Person {
        Id = (int) id,
        Title = name,
        IconName = "appbar_people"
      };

      Person person = PeopleList.FirstOrDefault(p => string.Compare(p.Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      Items.Insert(person == null ? 0 : Items.IndexOf(person), newPerson);
      PeopleList.Add(person);
      return newPerson;
    }

    public PeopleGroup CreateGroup(string name) {
      if (!Db.Execute($"insert into PeopleGroups (Name) values ('{name}')")) return null;
      var id = Db.GetLastIdFor("PeopleGroups");
      if (id == null) return null;
      PeopleGroup newGroup = new PeopleGroup {
        Id = (int)id,
        Title = name,
        IconName = "appbar_people_multiple"
      };

      var group = Items.FirstOrDefault(x => x is PeopleGroup && string.Compare(((PeopleGroup)x).Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      Items.Insert(group == null ? 0 : Items.IndexOf(group), newGroup);
      return newGroup;
    }

    public void DeletePerson(Person person) {
      Db.Execute($"delete from MediaItemPerson where PersonId = {person.Id}");
      Db.Execute($"delete from People where Id = {person.Id}");
      if (person.PeopleGroupId == -1) {
        Items.Remove(person);
      } else {
        var group = Items.FirstOrDefault(item => item is PeopleGroup && ((PeopleGroup) item).Id == person.PeopleGroupId);
        ((PeopleGroup) @group)?.Items.Remove(person);
      }
      PeopleList.Remove(person);
    }

    public void DeletePeopleGroup(PeopleGroup group) {
      Db.Execute($"update People set PeopleGroupId = null where PeopleGroupId = {group.Id}");
      Db.Execute($"delete from PeopleGroups where Id = {group.Id}");
      var g = Items.FirstOrDefault(x => x is PeopleGroup && ((PeopleGroup)x).Id == group.Id);
      if (g != null)
        Items.Remove(g);
    }
  }
}
