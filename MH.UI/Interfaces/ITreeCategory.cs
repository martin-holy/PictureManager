using MH.Utils.Interfaces;

namespace MH.UI.Interfaces; 

public interface ITreeCategory : ITreeItem {
  public int Id { get; }
  public bool CanCopyItem { get; set; }
  public bool CanMoveItem { get; set; }

  public void ItemCreate(ITreeItem parent);
  public void ItemRename(ITreeItem item);
  public void ItemDelete(ITreeItem item);

  public void GroupCreate(ITreeItem parent);
  public void GroupRename(ITreeGroup group);
  public void GroupDelete(ITreeGroup group);
  public void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest);

  public bool CanDrop(object src, ITreeItem dest);
  public void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy);
}

public interface ITreeCategory<TG> : ITreeCategory where TG : class, ITreeItem {
  public void SetGroupDataAdapter(ITreeDataAdapter<TG> gda);
}