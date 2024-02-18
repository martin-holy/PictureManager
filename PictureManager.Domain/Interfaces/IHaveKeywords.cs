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
}