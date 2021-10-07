namespace PictureManager.Domain.CatTreeViewModels {
  public interface ICatTreeViewCategory : ICatTreeViewItem {
    Category Category { get; }
    bool CanHaveGroups { get; set; }
    bool CanHaveSubItems { get; set; }
    bool CanCreateItems { get; set; }
    bool CanRenameItems { get; set; }
    bool CanDeleteItems { get; set; }
    bool CanCopyItem { get; set; }
    bool CanMoveItem { get; set; }

    IconName CategoryGroupIconName { get; }

    bool CanDrop(object src, ICatTreeViewItem dest);
    void OnDrop(object src, ICatTreeViewItem dest, bool aboveDest, bool copy);

    bool CanCreateItem(ICatTreeViewItem item);
    bool CanRenameItem(ICatTreeViewItem item);
    bool CanDeleteItem(ICatTreeViewItem item);
    bool CanSort(ICatTreeViewItem root);

    string ValidateNewItemTitle(ICatTreeViewItem root, string name);
    string GetTitle(ICatTreeViewItem item);
    void SetTitle(ICatTreeViewItem item, string title);
    ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name);
    void ItemRename(ICatTreeViewItem item, string name);
    void ItemDelete(ICatTreeViewItem item);
    void ItemCopy(ICatTreeViewItem item, ICatTreeViewItem dest);
    void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest);

    string ValidateNewGroupTitle(ICatTreeViewItem root, string name);
    ICatTreeViewGroup GroupCreate(ICatTreeViewCategory cat, string name);
    void GroupRename(ICatTreeViewGroup group, string name);
    void GroupDelete(ICatTreeViewGroup group);
    void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest);
  }
}
