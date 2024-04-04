using System;

namespace MH.Utils.Extensions;

public static class ArrayExtensions {
  public static T[] NullIfEmpty<T>(this T[] self) =>
    self?.Length > 0 ? self : null;

  public static void ForEach<T>(this T[] self, Action<T> action) {
    for (int i = 0; i < self.Length; i++)
      action(self[i]);
  }
}