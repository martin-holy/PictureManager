namespace MH.UI.WPF.Interfaces {
  public interface ICatTreeViewCategory : ICatTreeViewItem {
    bool CanCopyItem { get; set; }
    bool CanMoveItem { get; set; }

    void ItemCreate(ICatTreeViewItem root);
    void ItemRename(ICatTreeViewItem item);
    void ItemDelete(ICatTreeViewItem item);
    void ItemCopy(ICatTreeViewItem item, ICatTreeViewItem dest);
    void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest);

    void GroupCreate(ICatTreeViewItem root);
    void GroupRename(ICatTreeViewGroup group);
    void GroupDelete(ICatTreeViewGroup group);
    void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest);

    bool CanDrop(object src, ICatTreeViewItem dest);
    void OnDrop(object src, ICatTreeViewItem dest, bool aboveDest, bool copy);
  }
}
