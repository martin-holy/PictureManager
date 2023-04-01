using System.Windows.Input;

namespace PictureManager.Utils {
  public static class InputUtils {
    public static (bool isCtrlOn, bool isAltOn, bool isShiftOn) GetControlAltShiftModifiers() {
      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isAltOn = (Keyboard.Modifiers & ModifierKeys.Alt) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;

      return new(isCtrlOn, isAltOn, isShiftOn);
    }
  }
}
