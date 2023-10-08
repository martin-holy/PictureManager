using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.Extensions; 

public static class EnumerableExtensions {
  public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> items) =>
    items ?? Enumerable.Empty<T>();
}