namespace MH.Utils.Extensions;

public static class ArrayExtensions {
  public static T[] NullIfEmpty<T>(this T[] self) =>
    self?.Length > 0 ? self : null;
}