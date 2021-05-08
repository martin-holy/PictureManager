namespace PictureManager.Domain.CatTreeViewModels {
  public interface ICatTreeViewCategory: ICatTreeViewBaseItem {
    Category Category { get; }
    bool CanHaveGroups { get; set; }
    bool CanHaveSubItems { get; set; }
    bool CanModifyItems { get; set; }
    IconName CategoryGroupIconName { get; }
    string ValidateNewItemTitle(ICatTreeViewBaseItem root, string name);
    void ItemCreate(ICatTreeViewBaseItem root, string name);
    void ItemRename(ICatTreeViewBaseItem item, string name);
    void ItemDelete(ICatTreeViewBaseItem item);
    string ValidateNewGroupTitle(ICatTreeViewBaseItem root, string name);
    void GroupCreate(ICatTreeViewBaseItem root, string name);
    void GroupRename(ICatTreeViewGroup group, string newTitle);
    void GroupDelete(ICatTreeViewGroup group);
    void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest);
  }
}
