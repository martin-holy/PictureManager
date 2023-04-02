using System;

namespace PictureManager.Domain.Utils {
  public static class Keyboard {
    public static Func<bool> IsCtrlOn { get; set; }
    public static Func<bool> IsAltOn { get; set; }
    public static Func<bool> IsShiftOn { get; set; }
  }
}
