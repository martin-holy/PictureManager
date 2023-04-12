using System;

namespace MH.Utils.EventsArgs {
  public class RoutedEventArgs : EventArgs {
    public object OriginalSource { get; set; }
    public object DataContext { get; set; }
    public bool IsCtrlOn { get; set; }
    public bool IsAltOn { get; set; }
    public bool IsShiftOn { get; set; }
    public bool IsSourceDesired { get; set; }
  }
}
