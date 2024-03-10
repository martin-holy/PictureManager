using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Interfaces;

public interface IHaveKeywords {
    public List<KeywordM> Keywords { get; set; }
    public IEnumerable<KeywordM> GetKeywords() => Keywords.GetKeywords();
}

public static class HaveKeywordsExtensions {
  public static IEnumerable<KeywordM> GetKeywords(this IEnumerable<IHaveKeywords> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetKeywords())
      .Distinct();

  public static IEnumerable<T> GetBy<T>(this IEnumerable<T> items, KeywordM keyword, bool recursive) where T : IHaveKeywords {
    var arr = recursive ? keyword.Flatten().ToArray() : new[] { keyword };
    return items.Where(x => x.Keywords?.Any(k => arr.Any(ar => ReferenceEquals(ar, k))) == true);
  }
}