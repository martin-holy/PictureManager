using System.Collections.ObjectModel;

namespace PictureManager.Data {
  public class PeopleGroup: BaseItem {
    public ObservableCollection<Person> Items { get; set; }
    public DbStuff Db;
    public int Id;

    public PeopleGroup() {
      Items = new ObservableCollection<Person>();
    }

    public void Rename(DbStuff db, string newName) {
      db.Execute($"update PeopleGroups set Name = \"{newName}\" where Id = {Id}");
      Title = newName;
    }
  }
}
