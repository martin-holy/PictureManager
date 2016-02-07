using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace PictureManager.Data {
  public class People : BaseItem {
    public ObservableCollection<Person> Items { get; set; }
    public DbStuff Db;

    public People() {
      Items = new ObservableCollection<Person>();
    }

    public void Load() {
      Items.Clear();
      const string sql = "select Id, Name from People order by Name";
      foreach (DataRow row in Db.Select(sql)) {
        Items.Add(new Person {
          Id = (int) (long) row[0],
          Title = (string) row[1],
          IconName = "appbar_people"
        });
      }
    }

    public Person GetPersonByName(string name, bool create) {
      Person p = Items.FirstOrDefault(x => x.Title.Equals(name));
      if (p != null) return p;
      return create ? CreatePerson(name) : null;
    }

    public Person GetPersonById(int id) {
      return Items.FirstOrDefault(x => x.Id == id);
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

      Person person = Items.FirstOrDefault(p => string.Compare(p.Title, name, StringComparison.OrdinalIgnoreCase) >= 0);
      Items.Insert(person == null ? 0 : Items.IndexOf(person), newPerson);
      return newPerson;
    }

    public void DeletePerson(Person person) {
      Db.Execute($"delete from PicturePerson where PersonId = {person.Id}");
      Db.Execute($"delete from People where Id = {person.Id}");
      Items.Remove(person);
    }
  }
}
