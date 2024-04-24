using MH.Utils.Extensions;
using MovieManager.Plugins.Common.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MovieManager.Plugins.CSFDcz;

public static class Parser {
  public static SearchResult[] ParseSearch(string text) {
    var moviesRange = text.GetRangeBetween("<section class=\"box main-movies\">", "</section>");
    var movieResultRanges = text.GetRangeBetween("<article", "</article>", moviesRange);

    return movieResultRanges.Select(x => ParseSearchResult(text, x)).ToArray();
  }

  private static SearchResult ParseSearchResult(string text, Tuple<int, int> article) {
    var urlRange = text.GetRangeBetween("<a href=\"", "\"", article.Item1);
    var nameRange = text.GetRangeBetween("class=\"film-title-name\">", "</a>", urlRange.Item2);
    var yearRange = text.GetRangeBetween("<span class=\"info\">", "</span>", nameRange.Item2);
    var infoRange = text.GetRangeBetween("<span class=\"info\">", "</span>", yearRange.Item2);
    var actors = ExtractUrlAndText(text.GetFromRange(new(infoRange.Item2, article.Item2)));

    return new() {
      DetailId = new(ExtractId(text.GetFromRange(urlRange)), Core.IdName),
      Name = text.GetFromRange(nameRange),
      Year = ExtractYear(text, yearRange),
      Type = text.GetFromRange(infoRange),
      Desc = string.Join(", ", actors.Select(x => x.Item2))
    };
  }

  public static MovieDetail ParseMovie(string text, DetailId detailId) {
    var idx = 0;

    if (!text.TryIndexOf("film-info", ref idx, idx)) return null;

    var md = new MovieDetail {
      DetailId = detailId
    };

    if (text.GetRangeBetween("film-header-name", "<h1>", "</h1>", idx) is { } titleRange) {
      idx = titleRange.Item2;
      md.Title = text.GetFromRange(titleRange).Trim();
    }

    if (!text.TryIndexOf("film-info-content", ref idx, idx)) return null;

    // TODO film-posters

    if (text.GetRangeBetween("film-rating-average", ">", "</div>", idx) is { } ratingRange) {
      idx = ratingRange.Item2;
      md.Rating = ExtractRating(text, ratingRange);
    }

    if (text.GetRangeBetween("class=\"genres\"", ">", "</div>", idx) is { } genreRange) {
      idx = genreRange.Item2;
      md.Genres = ExtractGenres(text, genreRange);
    }

    return null;
  }

  private static string ExtractId(string text) {
    var match = Regex.Match(text, @"\d+");
    return match.Success ? match.Value : null;
  }

  private static int ExtractYear(string text, Tuple<int, int> yearRange) =>
    int.TryParse(text.GetFromRange(yearRange).Replace("(", string.Empty).Replace(")", string.Empty), out var yearInt) ? yearInt : 0;

  private static double ExtractRating(string text, Tuple<int, int> ratingRange) {
    var s = text.GetFromRange(ratingRange).Trim().Replace("%", string.Empty);
    return int.TryParse(s, out var i) ? i / 10.0 : 0;
  }

  private static string[] ExtractGenres(string text, Tuple<int, int> genreRange) {
    var genres = ExtractUrlAndText(text.GetFromRange(genreRange));
    // TODO translate
    return genres.Select(x => x.Item2).ToArray();
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