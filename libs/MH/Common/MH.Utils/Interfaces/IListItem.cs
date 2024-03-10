namespace MH.Utils.Interfaces {
  public interface IListItem : ISelectable {
    public bool IsHidden { get; set; }
    public bool IsIconHidden { get; set; }
    public bool IsNameHidden { get; set; }
    public string Icon { get; set; }
    public string Name { get; set; }
    public object Data { get; }
  }
}
