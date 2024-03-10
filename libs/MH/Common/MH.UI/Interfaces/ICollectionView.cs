namespace MH.UI.Interfaces; 

public interface ICollectionView : ITreeView {
  public bool CanOpen { get; set; }
  public bool CanSelect { get; set; }
  public void OpenItem(object item);
  public void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn);
  public void SetExpanded(object group);
}

public interface ICollectionViewGroup {
  public double Width { get; set; }
  public int GetItemSize(object item, bool getWidth);
}