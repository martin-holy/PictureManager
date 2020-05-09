namespace PictureManager.Domain.Models {
  public interface ICategoryItem {
    IconName CategoryGroupIconName { get; }
    string ValidateNewItemTitle(BaseTreeViewItem root, string name);
    void ItemCreate(BaseTreeViewItem root, string name);
    void ItemRename(BaseTreeViewItem item, string name);
    void ItemDelete(BaseTreeViewItem item);
  }
}
