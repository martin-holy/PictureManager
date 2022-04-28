namespace MH.Utils.Interfaces {
  public interface IListItem : ISelectable {
    bool IsHidden { get; set; }
    string Name { get; set; }
    string IconName { get; set; }
  }
}
