using System.Windows.Input;

namespace PictureManager.Utils {
  public static class InputUtils {
    public static (bool isCtrlOn, bool isShiftOn) GetKeyboardModifiers(MouseButtonEventArgs e) {
      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;

      // use middle and right button like CTRL + left button
      if (e.ChangedButton is MouseButton.Middle or MouseButton.Right) {
        isCtrlOn = true;
        isShiftOn = false;
      }

      return new(isCtrlOn, isShiftOn);
    }
  }
}
