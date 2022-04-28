namespace MH.Utils.Interfaces {
  public interface ITreeCategory : ITreeItem {
    bool CanCopyItem { get; set; }
    bool CanMoveItem { get; set; }

    void ItemCreate(ITreeItem root);
    void ItemRename(ITreeItem item);
    void ItemDelete(ITreeItem item);
    void ItemCopy(ITreeItem item, ITreeItem dest);
    void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest);

    void GroupCreate(ITreeItem root);
    void GroupRename(ITreeGroup group);
    void GroupDelete(ITreeGroup group);
    void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest);

    bool CanDrop(object src, ITreeItem dest);
    void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy);
  }
}
