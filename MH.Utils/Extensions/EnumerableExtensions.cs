using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.Extensions; 

public static class EnumerableExtensions {
  public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> items) =>
    items ?? Enumerable.Empty<T>();

  public static IEnumerable<int> ToHashCodes<T>(this IEnumerable<T> items) =>
    items?.Select(x => x.GetHashCode());

  public static string ToCsv<T>(this IEnumerable<T> items, string separator = ",") =>
    items == null
      ? string.Empty
      : string.Join(separator, items.Select(x => x.ToString()));
}