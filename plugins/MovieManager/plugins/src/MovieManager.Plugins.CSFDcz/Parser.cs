using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MH.Utils.Extensions;
using MovieManager.Plugins.Common.Models;

namespace MovieManager.Plugins.CSFDcz;

public static class Parser {
  public static SearchResult[] ParseSearch(string text) {
    var movies = text.GetRangeBetween("<section class=\"box main-movies\">", "</section>");
    var mArticles = text.GetRangeBetween("<article", "</article>", movies);
    var serials = text.GetRangeBetween("<section class=\"box main-series\">", "</section>", movies.Item2);
    var sArticles = text.GetRangeBetween("<article", "</article>", serials);

    foreach (var article in mArticles) {
      ParseArticle(text, article);
    }

    var a = text.GetFromRange(mArticles[0]);
    var b = ExtractUrlAndText(a);

    return [];
  }

  private static SearchResult ParseArticle(string text, Tuple<int, int> article) {
    var sr = new SearchResult();

    var name = text.GetRangeBetween("class=\"film-title-name\">", "</a>", article.Item1);
    var year = text.GetRangeBetween("<span class=\"info\">", "</span>", name.Item2);
    var info = text.GetRangeBetween("<span class=\"info\">", "</span>", year.Item2);
    var actors = ExtractUrlAndText(text.GetFromRange(new(info.Item2, article.Item2)));

    sr.Name = text.GetFromRange(name);
    if (int.TryParse(text.GetFromRange(year).Replace("(", string.Empty).Replace(")", string.Empty), out var yearInt))
      sr.Year = yearInt;
    
    sr.Desc = text.GetFromRange(info);

    return sr;
  }

  public static MovieDetail ParseMovie(string text) {
    return null;
  }

  private static string ExtractId(string text) {
    var match = Regex.Match(text, @"\d+");
    return match.Success ? match.Value : null;
  }

  private static Tuple<string, string>[] ExtractUrlAndText(string html) {
    var regex = new Regex(@"<a\s+href=""(?<url>[^""]+)""\s*>(?<text>[^<]+)</a>",
      RegexOptions.IgnoreCase | RegexOptions.Singleline);

    return regex.Matches(html)
      .Select(x => new Tuple<string, string>(
        x.Groups["url"].Value,
        x.Groups["text"].Value))
      .ToArray();
  }
}