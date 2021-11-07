namespace MH.UI.WPF.Interfaces {
  public interface ICatTreeViewCategory : ICatTreeViewItem {
    void ItemCreate(ICatTreeViewItem root);
    void ItemRename(ICatTreeViewItem item);
    void ItemDelete(ICatTreeViewItem item);

    void GroupCreate(ICatTreeViewItem root);
    void GroupRename(ICatTreeViewGroup group);
    void GroupDelete(ICatTreeViewGroup group);

    /*string ValidateNewGroupTitle(ICatTreeViewItem root, string name);
    string GetGroupTitle(ICatTreeViewGroup item);
    void SetGroupTitle(ICatTreeViewGroup item, string title);
    void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest);*/
  }
}
