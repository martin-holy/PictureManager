namespace PictureManager.ViewModel {
  public class Person : BaseTreeViewTagItem {
    public int? PeopleGroupId;
    public DataModel.Person Data;

    public Person() { }

    public Person(DataModel.Person data) {
      Data = data;
      Id = data.Id;
      Title = data.Name;
      IconName = "appbar_people";
      PeopleGroupId = data.PeopleGroupId;
    }
  }
}
