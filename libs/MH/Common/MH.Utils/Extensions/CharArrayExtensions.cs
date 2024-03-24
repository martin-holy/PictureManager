namespace MH.Utils.Extensions;

public static class CharArrayExtensions {
  public static bool Contains(this char[] array, string value) {
    for (int i = 0; i < array.Length - value.Length; i++) {
      var found = true;
      for (int j = 0; j < value.Length; j++) {
        if (array[i + j] == value[j]) continue;
        found = false;
        break;
      }

      if (found) return true;
    }

    return false;
  }
}