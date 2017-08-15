namespace PictureManager.ViewModel {
  public class SqlQuery : BaseTreeViewTagItem {
    public string Query;
    public DataModel.SqlQuery Data;

    public SqlQuery(DataModel.SqlQuery data) {
      Data = data;
      Id = data.Id;
      Title = data.Name;
      Query = data.Query;
      IconName = "appbar_location_checkin";
    }
  }
}