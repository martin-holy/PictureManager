using MH.Utils.Extensions;
using MovieManager.Plugins.Common.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MovieManager.Plugins.CSFDcz;

public static class Parser {
  public static SearchResult[] ParseSearch(string text) {
    var moviesRange = text.GetRangeBetween("<section class=\"box main-movies\">", null, "</section>");
    var movieResultRanges = text.GetRangeBetween("<article", null, "</article>", moviesRange);

    return movieResultRanges.Select(x => ParseSearchResult(text, x)).ToArray();
  }

  private static SearchResult ParseSearchResult(string text, Tuple<int, int> article) {
    var urlRange = text.GetRangeBetween("<a href=\"", null, "\"", article.Item1);
    var nameRange = text.GetRangeBetween("class=\"film-title-name\">", null, "</a>", urlRange.Item2);
    var yearRange = text.GetRangeBetween("<span class=\"info\">", null, "</span>", nameRange.Item2);
    var infoRange = text.GetRangeBetween("<span class=\"info\">", null, "</span>", yearRange.Item2);
    var actors = ExtractUrlAndText(text.GetFromRange(new(infoRange.Item2, article.Item2)));

    return new() {
      DetailId = new(ExtractFirstInt(text.GetFromRange(urlRange)), Core.IdName),
      Name = text.GetFromRange(nameRange),
      Year = ExtractSearchYear(text, yearRange),
      Type = text.GetFromRange(infoRange),
      Desc = string.Join(", ", actors.Select(x => x.Item2))
    };
  }

  public static MovieDetail ParseMovie(string text, DetailId detailId) {
    var idx = 0;

    if (!text.TryIndexOf("film-info", ref idx)) return null;

    var md = new MovieDetail {
      DetailId = detailId
    };

    // title
    if (text.GetRangeBetween("film-header-name", "<h1>", "</h1>", idx) is { } titleRange) {
      idx = titleRange.Item2;
      md.Title = text.GetFromRange(titleRange).Trim();
    }

    // poster
    if (text.GetRangeBetween("film-posters", "src=\"//", "\"", idx) is { } posterRange)
      md.Poster = new() { Url = ExtractImage(text.GetFromRange(posterRange)) };

    if (!text.TryIndexOf("film-info-content", ref idx)) return null;

    // rating
    if (text.GetRangeBetween("film-rating-average", ">", "</div>", idx) is { } ratingRange) {
      idx = ratingRange.Item2;
      md.Rating = ExtractRating(text, ratingRange);
    }

    // genres
    if (text.GetRangeBetween("class=\"genres\"", ">", "</div>", idx) is { } genreRange) {
      idx = genreRange.Item2;
      md.Genres = ExtractGenres(text, genreRange);
    }

    // year and length
    if (text.GetRangeBetween("class=\"origin\"", "<span>", "</span>", idx) is { } yearRange) {
      md.Year = int.TryParse(ExtractFirstInt(text.GetFromRange(yearRange)), out var yearInt) ? yearInt : 0;

      if (text.GetRangeBetween(">", null, ">", yearRange.Item2) is { } lengthRange)
        md.Runtime = int.TryParse(ExtractFirstInt(text.GetFromRange(lengthRange)), out var lengthInt) ? lengthInt : 0;
    }

    // cast
    if (text.TryIndexOf("id=\"creators\"", ref idx)) {
      if (text.GetRangeBetween("Hrají:", ">", "</span>", idx) is { } actorsRange) {
        var actorLinks = ExtractUrlAndText(text.GetFromRange(actorsRange));
        md.Cast = actorLinks
          .Select(x => new Actor {
            DetailId = new(ExtractFirstInt(x.Item1), Core.IdName),
            Name = x.Item2
          })
          .Select(x => new Cast { Actor = x, Characters = [] })
          .ToArray();
      }
    }

    return null;
  }

  private static string ExtractFirstInt(string text) {
    var match = Regex.Match(text, @"\d+");
    return match.Success ? match.Value : null;
  }

  private static int ExtractSearchYear(string text, Tuple<int, int> yearRange) =>
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

  /// <summary>
  /// Removes the resized part of the image url
  /// </summary>
  private static string ExtractImage(string url) {
    var parts = url.Split('/');
    return string.Join("/", parts.Take(1).Concat(parts.Skip(4)));
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