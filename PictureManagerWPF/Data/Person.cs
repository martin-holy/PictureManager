namespace PictureManager.Data {
  public class Person : BaseTagItem {
    public void Rename(DbStuff db, string newName) {
      db.Execute($"update People set Name = \"{newName}\" where Id = {Id}");
      Title = newName;
    }
  }
}
