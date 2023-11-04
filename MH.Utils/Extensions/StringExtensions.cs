using System.Linq;

namespace MH.Utils.Extensions {
  public static class StringExtensions {
    public static int IntParseOrDefault(this string s, int d) =>
      int.TryParse(s, out var result) ? result : d;

    public static int FirstIndexOfLetter(this string s) {
      var index = 0;
      while (s.Length - 1 > index) {
        if (char.IsLetter(s, index))
          break;
        index++;
      }

      return index;
    }

    public static bool TryParseDoubleUniversal(this string s, out double result) {
      result = 0.0;
      if (string.IsNullOrEmpty(s)) return false;

      var clean = new string(s.Where(x => char.IsDigit(x) || x == '.' || x == ',' || x == '-').ToArray());
      var iOfSep = clean.LastIndexOfAny(new[] { ',', '.' });
      var partA = clean.Substring(0, iOfSep).Replace(",", string.Empty).Replace(".", string.Empty);
      var partB = clean.Substring(iOfSep + 1);
      if (!int.TryParse(partA, out var intA)) return false;
      if (!int.TryParse(partB, out var intB)) return false;
      if (intA < 0) intB *= -1;
      var dp = double.Parse("1".PadRight(partB.Length + 1, '0'));

      result = intA + intB / dp;
      return true;
    }

    /// <summary>
    /// Replaces first string format item with count and second with 's' if count > 1.
    /// </summary>
    public static string Plural(this string s, int count) =>
      string.Format(s, count, count > 1 ? "s" : string.Empty);
  }
}
