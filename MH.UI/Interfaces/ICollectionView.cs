namespace MH.UI.Interfaces; 

public interface ICollectionView : ITreeView {
  public void OpenItem(object item);
  public void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn);
  public void SetExpanded(object group);
}