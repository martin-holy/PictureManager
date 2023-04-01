namespace MH.Utils.EventsArgs {
  public class ClickEventArgs {
    public object OriginalSource { get; set; }
    public object DataContext { get; set; }
    public int ClickCount { get; set; }
    public bool IsCtrlOn { get; set; }
    public bool IsAltOn { get; set; }
    public bool IsShiftOn { get; set; }

    public ClickEventArgs() { }
  }
}
