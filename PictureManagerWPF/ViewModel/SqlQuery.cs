namespace PictureManager.ViewModel {
  public class SqlQuery : BaseTreeViewItem, IDbItem {
    public DataModel.SqlQuery Data;
    public override string Title { get => Data.Name; set { Data.Name = value; OnPropertyChanged(); } }

    public SqlQuery(DataModel.SqlQuery data) {
      Data = data;
      IconName = IconName.DatabaseSql;
    }
  }
}