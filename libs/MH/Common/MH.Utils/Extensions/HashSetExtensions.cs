using System.Collections.Generic;

namespace MH.Utils.Extensions {
  public static class HashSetExtensions {
    public static bool Toggle<T>(this HashSet<T> hashSet, T item) {
      if (hashSet.Remove(item))
        return false;

      hashSet.Add(item);
      return true;
    }
  }
}
