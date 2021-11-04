namespace MH.Utils.Extensions {
  public static class StringExtensions {
    public static int IntParseOrDefault(this string s, int d) => int.TryParse(s, out var result) ? result : d;

    public static int FirstIndexOfLetter(this string s) {
      var index = 0;
      while (s.Length - 1 > index) {
        if (char.IsLetter(s, index))
          break;
        index++;
      }

      return index;
    }
  }
}
