using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MH.Utils.Extensions;
using MovieManager.Plugins.Common.Models;

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


  public static SearchResult[] ParseSearch(string text) {
    var idx = 0;

    if (!text.TryIndexOf("id=\"hle_film\"", ref idx, idx)) return null;

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
    if (!text.TryIndexOf("class=\"v_box\"", ref idx, idx)
        || !text.TryIndexOf("class=\"info\"", ref idx, idx)) return null;

    var sr = new SearchResult();
    var aRange = text.GetRangeBetween("<a", null, "</a", idx);

    if (text.GetRangeBetween("rel=\"", null, "\"", aRange.Item1) is { } imageRange)
      if (imageRange.Item1 < aRange.Item2)
        sr.Image = new() { Url = text.GetFromRange(imageRange) };

    if (text.GetRangeBetween("href=\"", "film/", "\"", aRange.Item1) is { } urlRange)
      sr.DetailId = UrlToDetailId(text.GetFromRange(urlRange));

    if (text.GetRangeBetween(">", null, "<", aRange.Item1) is { } nameRange)
      sr.Name = text.GetFromRange(nameRange);

    if (text.GetRangeBetween("<span", ">", "<", idx) is { } yearAndTypeRange) {
      idx = yearAndTypeRange.Item2;
      var yearType = ParseYearTypeSearchResult(text.GetFromRange(yearAndTypeRange));
      sr.Year = yearType.Item1;
      sr.Type = yearType.Item2;
    }

    if (text.GetRangeBetween("class=\"info\"", ">", "</div>", idx) is { } descRange)
      sr.Desc = text.GetFromRange(descRange).Replace("<br/>", " ");

    return sr;
  }

  private static Tuple<int, string> ParseYearTypeSearchResult(string text) {
    var year = text.GetFromRange(text.GetRangeBetween("(", null, ")"));
    var type = text.GetFromRange(text.GetRangeBetween("[", null, "]"));
    var yearInt = int.TryParse(year, out var yi) ? yi : 0;
    return new(yearInt, type);
  }

  public static MovieDetail ParseMovie(string text, DetailId detailId) {
    var idx = 0;

    if (!text.TryIndexOf("zakladni_info", ref idx, idx)) return null;

    var md = new MovieDetail {
      DetailId = detailId
    };

    // title
    if (text.GetRangeBetween("<a", ">", "</a", idx) is { } titleRange) {
      idx = titleRange.Item2;
      md.Title = text.GetFromRange(titleRange);
    }

    // poster
    if (text.GetRangeBetween("boxPlakaty", "src=\"", "\"", idx) is { } posterRange) {
      md.Poster = new() { Url = text.GetFromRange(posterRange) };
    }

    // genres
    if (text.GetRangeBetween("Žánr:", null, "clear_text", idx) is { } genresRange) {
      idx = genresRange.Item2;
      md.Genres = ParseGenres(text, genresRange);
    }

    // year
    if (text.GetRangeBetween("Rok:", "right_text\">", "<", idx) is { } yearRange) {
      idx = yearRange.Item2;
      md.Year = int.TryParse(text.GetFromRange(yearRange), out var yearInt) ? yearInt : 0;
    }

    // runtime
    if (text.GetRangeBetween("Délka:", "right_text\">", " ", idx) is { } runtimeRange) {
      idx = runtimeRange.Item2;
      md.Runtime = int.TryParse(text.GetFromRange(runtimeRange), out var runtimeInt) ? runtimeInt : 0;
    }

    // rating
    if (text.GetRangeBetween("hodnoceni\">", ">", "<", idx) is { } ratingRange) {
      idx = ratingRange.Item2;
      md.Rating = ExtractRating(text, ratingRange);
    }

    // plot
    if (text.GetRangeBetween("id=\"zbytek\">", null, "<a", idx) is { } plotRange) {
      idx = plotRange.Item2;
      md.Plot = text.GetFromRange(plotRange).Replace("<br />", string.Empty);
    }

    // cast
    if (text.TryIndexOf(">hraje:<", ref idx, idx))
      if (text.GetRangeBetween("<table", ">", "</table", idx) is { } castRange)
        md.Cast = text.GetRangeBetween("<tr", null, "</tr", castRange)
          .Select(x => ParseCast(text, x))
          .Where(x => x != null)
          .ToArray();

    //Poster, Images

    return md;
  }

  private static string[] ParseGenres(string text, Tuple<int, int> range) =>
    text.GetRangeBetween("<a", null, ">", range)
      .Select(x => ExtractFirstInt(text.GetFromRange(x)))
      .Where(x => x != null)
      .Select(x => _genres[int.Parse(x)])
      .ToArray();

  private static double ExtractRating(string text, Tuple<int, int> ratingRange) {
    var s = text.GetFromRange(ratingRange).Trim().Replace("%", string.Empty);
    return double.TryParse(s, CultureInfo.InvariantCulture, out var d) ? d / 10.0 : 0;
  }

  private static Cast ParseCast(string text, Tuple<int, int> castRange) {
    var actor = new Actor();
    var cast = new Cast { Actor = actor };
    var idx = castRange.Item1;

    // TODO add endIndex to GetRangeBetween method
    // image
    if (text.GetRangeBetween("class=\"photo", "rel=\"", "\"", idx) is { } imageRange
        && imageRange.Item1 < castRange.Item2) {
      idx = imageRange.Item2;
      actor.Image = new() { Url = text.GetFromRange(imageRange) };
    }

    // id
    if (text.GetRangeBetween("class=\"nazev", "lidi/", ".", idx) is { } idRange
        && idRange.Item1 < castRange.Item2) {
      idx = idRange.Item2;
      actor.DetailId = new(text.GetFromRange(idRange), Core.IdName);
    }

    // name
    if (text.GetRangeBetween(">", null, "<", idx) is { } nameRange
        && nameRange.Item1 < castRange.Item2) {
      idx = nameRange.Item2;
      actor.Name = text.GetFromRange(nameRange);
    }

    // characters
    if (text.GetRangeBetween("role=", ">", "<", idx) is { } charactersRange
        && charactersRange.Item1 < castRange.Item2) {
      cast.Characters = [text.GetFromRange(charactersRange)];
    }

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