namespace PictureManager.ViewModel {
  public class PeopleGroup : BaseTreeViewTagItem {
    public DataModel.PeopleGroup Data;

    public PeopleGroup(DataModel.PeopleGroup data) {
      Data = data;
      Id = data.Id;
      Title = data.Name;
      IconName = "appbar_people_multiple";
    }
  }
}
