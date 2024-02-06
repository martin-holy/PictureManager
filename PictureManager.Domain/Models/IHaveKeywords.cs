using MH.Utils.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models;

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
}