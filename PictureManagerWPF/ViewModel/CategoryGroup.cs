namespace PictureManager.ViewModel {
  public class CategoryGroup : BaseTreeViewTagItem {
    public DataModel.CategoryGroup Data;
    public Categories Category;

    public CategoryGroup(DataModel.CategoryGroup data) {
      Data = data;
      Id = data.Id;
      Title = data.Name;
      Category = (Categories) data.Category;
    }
  }
}
