namespace PictureManager.ViewModel {
  public class CategoryGroup : BaseTreeViewTagItem, IDbItem {
    public DataModel.CategoryGroup Data;
    public override string Title { get => Data.Name; set { Data.Name = value; OnPropertyChanged(); } }
    public Categories Category => (Categories) Data.Category;

    public CategoryGroup(DataModel.CategoryGroup data) {
      Data = data;
    }
  }
}