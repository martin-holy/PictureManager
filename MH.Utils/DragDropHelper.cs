namespace MH.Utils {
  public enum DragDropEffects {
    Scroll = int.MinValue,
    All = -2147483645,
    None = 0,
    Copy = 1,
    Move = 2,
    Link = 4
  }

  public static class DragDropHelper {
    public delegate object CanDragFunc(object source);
    public delegate DragDropEffects CanDropFunc(object target, object data, bool haveSameOrigin);
    public delegate void DoDropAction(object data, bool haveSameOrigin);
  }
}