namespace PictureManager.ViewModel {
  public class Person : BaseTreeViewTagItem, IDbItem {
    public DataModel.Person Data;
    public override string Title { get => Data.Name; set { Data.Name = value; OnPropertyChanged(); } }

    public Person(DataModel.Person data) {
      Data = data;
      IconName = "appbar_people";
    }
  }
}