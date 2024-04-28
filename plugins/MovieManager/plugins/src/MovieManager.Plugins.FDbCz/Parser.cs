using MH.Utils;
using MH.Utils.Extensions;
using MovieManager.Plugins.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MovieManager.Plugins.FDbCz;

public static class Parser {
  private static readonly Dictionary<int, string> _genres = new() {
    { 86, "Adaptation" }, { 64, "Agitprop" }, { 3, "Action" }, { 90, "Actuality" }, { 63, "Allegorical Images" },
    { 8, "Animated" }, { 76, "Autobiographical" }, { 43, "Ballad" }, { 65, "Ballet" }, { 113, "Biblical" },
    { 140, "Burlesque Comedy" }, { 172, "Travelogue" }, { 2, "Crazy" }, { 133, "Cycle" }, { 19, "Black Comedy" },
    { 10, "Detective" }, { 11, "Children's" }, { 169, "Discussion" }, { 5, "Adventure" }, { 38, "Documentary" },
    { 137, "Documentary - Drama" }, { 9, "Drama" }, { 102, "Epic" }, { 4, "Erotic" }, { 123, "Etude" },
    { 158, "Experimental" }, { 12, "Fantasy" }, { 51, "Fairy Tale" }, { 163, "Film-Noir" }, { 75, "Film Poem" },
    { 127, "Film Essay" }, { 85, "Film Collage" }, { 101, "Philosophical" }, { 58, "Farce" }, { 14, "Gangster" },
    { 1, "Grotesque" }, { 16, "Historical" }, { 15, "Horror" }, { 96, "Dark Comedy" }, { 135, "Bitter Romance" },
    { 13, "Musical" }, { 61, "Song Illustration" }, { 138, "Production" }, { 17, "Catastrophic" }, { 83, "Combined" },
    { 7, "Comedy" }, { 46, "Comics" }, { 74, "Animated" }, { 18, "Crime" }, { 60, "Legend" }, { 166, "Literary" },
    { 117, "Puppet" }, { 21, "Lyrical" }, { 168, "Medallion" }, { 22, "Melodrama" }, { 23, "Romantic" },
    { 24, "Musical" }, { 147, "Mysterious" }, { 126, "Mystical" }, { 128, "Mythology" }, { 81, "Religious" },
    { 25, "Educational" }, { 59, "Opera" }, { 57, "Educational" }, { 68, "Parody" }, { 104, "Tape" }, { 44, "Parable" },
    { 71, "Poetic" }, { 167, "Poetry" }, { 26, "Fairytale" }, { 79, "Political" }, { 66, "Popular Science" },
    { 27, "Porn" }, { 28, "Story" }, { 56, "Promotional" }, { 136, "Transfer" }, { 29, "Story" },
    { 100, "Natural History" }, { 6, "Psychological" }, { 160, "Journalistic" }, { 173, "Reality Show" },
    { 55, "Advertisement" }, { 52, "Relaxation" }, { 42, "Retro" }, { 80, "Road Movie" }, { 31, "Family" },
    { 32, "Romantic" }, { 121, "Saga" }, { 69, "Satire" }, { 33, "Sci-Fi" }, { 88, "Situation Grotesque" },
    { 89, "Situation Comedy" }, { 119, "Sad Comedy" }, { 84, "Social" }, { 170, "Competitive" },
    { 103, "Social-Historical" }, { 87, "Social-Critical" }, { 77, "Sports" }, { 47, "Editing" }, { 73, "Symbolic" },
    { 40, "Spy" }, { 175, "Talk Show" }, { 45, "Dance" }, { 174, "Telenovela" }, { 34, "Thriller" }, { 78, "Tragedy" },
    { 41, "Tragicomedy" }, { 125, "Trick" }, { 35, "War" }, { 171, "Music Video" }, { 49, "Military" },
    { 124, "Artistic" }, { 36, "Western" }, { 165, "Entertaining" }, { 82, "Operetta" }, { 92, "Living Pictures" },
    { 37, "Biographical" }
  };

  private static StringRange _srSearchA = new("<a", "</a");
  private static StringRange _srSearchImage = new("rel=\"", "\"");
  private static StringRange _srSearchDetailId = new("href=", "film/", "\"");
  private static StringRange _srSearchName = new(">", "<");
  private static StringRange _srSearchYearAndType = new("<span", ">", "<");
  private static StringRange _srSearchYear = new("(", ")");
  private static StringRange _srSearchType = new("[", "]");
  private static StringRange _srSearchDesc = new("class=\"info\"", ">", "</div");

  private static StringRange _srDetailTitle = new("<a", ">", "</a");
  private static StringRange _srDetailPoster = new("boxPlakaty", "src=\"", "\"");
  private static StringRange _srDetailGenres = new("Žánr:", "clear_text");
  private static StringRange _srDetailGenre = new("<a", ">");
  private static StringRange _srDetailYear = new("Rok:", "right_text\">", "<");
  private static StringRange _srDetailRuntime = new("Délka:", "right_text\">", " ");
  private static StringRange _srDetailRating = new("hodnoceni\">", ">", "%");
  private static StringRange _srDetailPlot = new("id=\"zbytek\">", "<a");
  private static StringRange _srDetailCasts = new("<table", ">", "</table");
  private static StringRange _srDetailCast = new("<tr", "</tr");
  private static StringRange _srDetailCastImage = new("class=\"photo", "rel=\"", "\"");
  private static StringRange _srDetailCastId = new("class=\"nazev", "lidi/", ".");
  private static StringRange _srDetailCastName = new(">", "<");
  private static StringRange _srDetailCastCharacters = new("role=", ">", "<");

  public static SearchResult[] ParseSearch(string text) {
    var idx = 0;

    if (!text.TryIndexOf("id=\"hle_film", ref idx)) return null;

    var results = new List<SearchResult>();
    
    while (true) {
      if (ParseSearchResult(text, ref idx) is { } result)
        results.Add(result);
      else
        break;
    }

    return [.. results];
  }

  private static SearchResult ParseSearchResult(string text, ref int idx) {
    if (!text.TryIndexOf("class=\"v_box\"", ref idx)
        || !text.TryIndexOf("class=\"info\"", ref idx)) return null;

    var sr = new SearchResult();

    if (_srSearchA.From(text, idx)) {
      if (_srSearchImage.From(text, _srSearchA, out var imgUrl))
        sr.Image = new() { Url = imgUrl };

      if (_srSearchDetailId.From(text, _srSearchA, out var detailUrl))
        sr.DetailId = UrlToDetailId(detailUrl);

      if (_srSearchName.From(text, _srSearchA, out var name))
        sr.Name = name;
    }

    if (_srSearchYearAndType.From(text, idx)) {
      idx = _srSearchYearAndType.EndIndex;
      var yearType = ParseYearTypeSearchResult(_srSearchYearAndType.AsString(text));
      sr.Year = yearType.Item1;
      sr.Type = yearType.Item2;
    }

    if (_srSearchDesc.From(text, idx))
      sr.Desc = _srSearchDesc.AsString(text).Replace("<br/>", " ");

    return sr;
  }

  private static Tuple<int, string> ParseYearTypeSearchResult(string text) {
    var year = _srSearchYear.From(text)?.AsString(text);
    var type = _srSearchType.From(text)?.AsString(text);
    var yearInt = int.TryParse(year, out var yi) ? yi : 0;
    return new(yearInt, type);
  }

  public static MovieDetail ParseMovie(string text, DetailId detailId) {
    var idx = 0;

    if (!text.TryIndexOf("zakladni_info", ref idx)) return null;

    var md = new MovieDetail {
      DetailId = detailId
    };

    md.Title = _srDetailTitle.From(text, ref idx)?.AsString(text);

    // TODO bigger poster
    if (_srDetailPoster.From(text, idx))
      md.Poster = new() { Url = _srDetailPoster.AsString(text) };

    md.Genres = ParseGenres(text, _srDetailGenres.From(text, ref idx));
    md.Year = _srDetailYear.From(text, ref idx)?.AsInt32(text) ?? 0;
    md.Runtime = _srDetailRuntime.From(text, ref idx)?.AsInt32(text) ?? 0;
    md.Rating = ExtractRating(text, _srDetailRating.From(text, ref idx));
    md.Plot = _srDetailPlot.From(text, ref idx)?.AsString(text).Replace("<br />", string.Empty);

    if (text.TryIndexOf(">hraje:<", ref idx) && _srDetailCasts.From(text, idx)) {
      md.Cast = _srDetailCasts
        .AsEnumerable(text, _srDetailCast)
        .Select(x => ParseCast(text, x))
        .Where(x => x != null)
        .ToArray();
    }

    //TODO Images

    return md;
  }

  private static string[] ParseGenres(string text, StringRange? range) {
    var genres = range?.AsStrings(text, _srDetailGenre)
      .Select(ExtractFirstInt)
      .Where(x => x != null)
      .Select(x => _genres[int.Parse(x)])
      .ToArray();

    return genres?.Length > 0 ? genres : [];
  }

  private static double ExtractRating(string text, StringRange? range) =>
    range != null && double.TryParse(range.Value.AsString(text), CultureInfo.InvariantCulture, out var d) ? d / 10.0 : 0;

  private static Cast ParseCast(string text, StringRange range) {
    var actor = new Actor();
    var cast = new Cast { Actor = actor };
    var idx = range.StartIndex;

    if (_srDetailCastImage.From(text, ref idx, range.EndIndex)?.AsString(text) is { } imgUrl)
      actor.Image = new() { Url = imgUrl };

    if (_srDetailCastId.From(text, ref idx, range.EndIndex)?.AsString(text) is { } id)
      actor.DetailId = new(id, Core.IdName);

    actor.Name = _srDetailCastName.From(text, ref idx, range.EndIndex)?.AsString(text).Replace("  ", " ");
    cast.Characters = _srDetailCastCharacters.From(text, ref idx)?.AsString(text).Split("/") ?? [];

    return cast;
  }

  private static string ExtractFirstInt(string text) {
    var match = Regex.Match(text, @"\d+");
    return match.Success ? match.Value : null;
  }

  public static string DetailIdToUrl(DetailId detailId) =>
    detailId.Id.Replace("_", "/");

  private static DetailId UrlToDetailId(string url) =>
    new(url.Replace("/", "_"), Core.IdName);
}