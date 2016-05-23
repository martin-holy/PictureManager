﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PictureManager.Dialogs;

namespace PictureManager.ViewModel {
  public class People : BaseTreeViewItem {
    public List<Person> AllPeople; 
    private static readonly Mutex Mut = new Mutex();

    public People() {
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
      //Add PeopleGroups
      foreach (var pg in ACore.Db.PeopleGroups.OrderBy(x => x.Name).Select(x => new PeopleGroup(x))) {
        //Add People to the PeopleGroup
        foreach (var p in ACore.Db.People.Where(x => x.PeopleGroupId == pg.Id).OrderBy(x => x.Name).Select(x => new Person(x))) {
          pg.Items.Add(p);
          AllPeople.Add(p);
        }
        Items.Add(pg);
      }

      //Add People without group
      foreach (var p in ACore.Db.People.Where(x => x.PeopleGroupId == null).OrderBy(x => x.Name).Select(x => new Person(x))) {
        Items.Add(p);
        AllPeople.Add(p);
      }
    }

    public Person GetPerson(int id) {
      return AllPeople.SingleOrDefault(x => x.Id == id);
    }

    public Person GetPerson(int id, int? peopleGroupId) {
      if (peopleGroupId != null) {
        var g = Items.OfType<PeopleGroup>().Single(x => x.Id == peopleGroupId);
        return g.Items.Cast<Person>().Single(x => x.Id == id);
      }
      return Items.OfType<Person>().Single(x => x.Id == id);
    }

    public Person GetPerson(string name, bool create) {
      if (create) Mut.WaitOne();
      var dmPerson = ACore.Db.People.SingleOrDefault(x => x.Name.Equals(name));
      if (dmPerson != null) {
        Mut.ReleaseMutex();
        return GetPerson(dmPerson.Id, dmPerson.PeopleGroupId);
      }
      if (!create) return null;
      var newPerson = CreatePerson(name, null);
      Mut.ReleaseMutex();
      return newPerson;
    }

    public Person CreatePerson(string name, PeopleGroup peopleGroup) {
      var dmPerson = new DataModel.Person {
        Id = ACore.Db.GetNextIdFor<DataModel.Person>(),
        Name = name,
        PeopleGroupId = peopleGroup?.Id
      };

      ACore.Db.Insert(dmPerson);

      var vmPerson = new Person(dmPerson);
      AllPeople.Add(vmPerson);
      SetInPalce(vmPerson, true);
      return vmPerson;
    }

    public PeopleGroup CreateGroup(string name) {
      var dmPeopleGroup = new DataModel.PeopleGroup {
        Id = ACore.Db.GetNextIdFor<DataModel.PeopleGroup>(),
        Name = name
      };

      ACore.Db.InsertOnSubmit(dmPeopleGroup);
      ACore.Db.SubmitChanges();

      var vmPeopleGroup = new PeopleGroup(dmPeopleGroup);
      SetInPalce(vmPeopleGroup, true);
      return vmPeopleGroup;
    }

    public void NewOrRenamePerson(Person person, PeopleGroup peopleGroup, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = ACore.WMain,
        IconName = "appbar_people",
        Title = rename ? "Rename Person" : "New Person",
        Question = rename ? "Enter the new name of the person." : "Enter the name of the new person.",
        Answer = rename ? person.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.Answer, person.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (ACore.Db.People.SingleOrDefault(x => x.Name.Equals(inputDialog.Answer)) != null) {
          inputDialog.ShowErrorMessage("Person's name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          person.Title = inputDialog.Answer;
          person.Data.Name = inputDialog.Answer;
          ACore.Db.UpdateOnSubmit(person.Data);
          ACore.Db.SubmitChanges();
          SetInPalce(person, false);
        } else CreatePerson(inputDialog.Answer, peopleGroup);
      }
    }

    public void NewOrRenameGroup(PeopleGroup peopleGroup, bool rename) {
      InputDialog inputDialog = new InputDialog {
        Owner = ACore.WMain,
        IconName = "appbar_people_multiple",
        Title = rename ? "Rename Group" : "New Group",
        Question = rename ? "Enter the new name for the group." : "Enter the name of the new group.",
        Answer = rename ? peopleGroup.Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (rename && string.Compare(inputDialog.TxtAnswer.Text, peopleGroup.Title, StringComparison.OrdinalIgnoreCase) == 0) {
          inputDialog.DialogResult = true;
          return;
        }

        if (ACore.Db.PeopleGroups.SingleOrDefault(x => x.Name.Equals(inputDialog.TxtAnswer.Text)) != null) {
          inputDialog.ShowErrorMessage("Group's name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        if (rename) {
          peopleGroup.Title = inputDialog.Answer;
          peopleGroup.Data.Name = inputDialog.Answer;
          ACore.Db.UpdateOnSubmit(peopleGroup.Data);
          ACore.Db.SubmitChanges();
          SetInPalce(peopleGroup, false);
        } else CreateGroup(inputDialog.Answer);
      }
    }

    public void SetInPalce(Person person, bool isNew) {
      var idx = ACore.Db.People.Where(x => x.PeopleGroupId == person.PeopleGroupId).OrderBy(x => x.Name).ToList().IndexOf(person.Data);
      if (person.PeopleGroupId == null) {
        idx += Items.OfType<PeopleGroup>().Count();
        if (isNew)
          Items.Insert(idx, person);
        else
          Items.Move(Items.IndexOf(person), idx);
      } else {
        var g = Items.OfType<PeopleGroup>().Single(x => x.Id == person.PeopleGroupId);
        if (isNew)
          g.Items.Insert(idx, person);
        else 
          g.Items.Move(g.Items.IndexOf(person), idx);
      }
    }

    public void SetInPalce(PeopleGroup peopleGroup, bool isNew) {
      var idx = ACore.Db.PeopleGroups.OrderBy(x => x.Name).ToList().IndexOf(peopleGroup.Data);
      if (isNew)
        Items.Insert(idx, peopleGroup);
      else 
        Items.Move(Items.IndexOf(peopleGroup), idx);
    }

    public void DeletePerson(Person person) {
      foreach (var mip in ACore.Db.MediaItemPeople.Where(x => x.PersonId == person.Id)) {
        ACore.Db.DeleteOnSubmit(mip);
      }

      ACore.Db.DeleteOnSubmit(person.Data);
      ACore.Db.SubmitChanges();

      if (person.PeopleGroupId == null) {
        Items.Remove(person);
      } else {
        Items.OfType<PeopleGroup>().Single(x => x.Id == person.PeopleGroupId).Items.Remove(person);
      }

      AllPeople.Remove(person);
    }

    public void DeletePeopleGroup(PeopleGroup group) {
      foreach (var p in ACore.Db.People.Where(x => x.PeopleGroupId == group.Id)) {
        p.PeopleGroupId = null;
      }

      ACore.Db.DeleteOnSubmit(group.Data);
      ACore.Db.SubmitChanges();

      foreach (var person in group.Items.Cast<Person>()) {
        person.PeopleGroupId = null;
        Items.Add(person);
      }

      foreach (var person in group.Items.Cast<Person>()) {
        SetInPalce(person, false);
      }
      
      group.Items.Clear();
      Items.Remove(group);
    }

    public void MovePerson(Person person, PeopleGroup peopleGroup) {
      if (person.PeopleGroupId == null) {
        Items.Remove(person);
      } else {
        Items.OfType<PeopleGroup>().Single(x => x.Id == person.PeopleGroupId).Items.Remove(person);
      }
      var newGroupId = peopleGroup?.Id;
      person.PeopleGroupId = newGroupId;
      person.Data.PeopleGroupId = newGroupId;
      ACore.Db.UpdateOnSubmit(person.Data);
      ACore.Db.SubmitChanges();
      SetInPalce(person, true); //person is new in the group
    }
  }
}
