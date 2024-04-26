using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MH.Utils.Extensions;
using MovieManager.Plugins.Common.Models;

namespace MovieManager.Plugins.FDbCz;

public static class Parser {
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

    // TODO if result doesn't have image
    if (text.GetRangeBetween("rel=\"", null, "\"", idx) is { } imageRange) {
      idx = imageRange.Item2;
      sr.Image = new() { Url = text.GetFromRange(imageRange) };
    }

    if (text.GetRangeBetween("href=\"", null, "\"", idx) is { } urlRange) {
      idx = urlRange.Item2;
      sr.DetailId = new(text.GetFromRange(urlRange), Core.IdName);
    }

    if (text.GetRangeBetween(">", null, "<", idx) is { } nameRange) {
      idx = nameRange.Item2;
      sr.Name = text.GetFromRange(nameRange);
    }

    if (text.GetRangeBetween("<span", ">", "<", idx) is { } yearAndTypeRange) {
      idx = yearAndTypeRange.Item2;
      if (ParseYearTypeSearchResult(text.GetFromRange(yearAndTypeRange)) is { } yearType) {
        sr.Year = yearType.Item1;
        sr.Type = yearType.Item2;
      }
    }

    if (text.GetRangeBetween("class=\"info\"", ">", "</div>", idx) is { } descRange) {
      sr.Desc = text.GetFromRange(descRange).Replace("<br/>", " ");
    }

    return sr;
  }

  private static Tuple<int, string> ParseYearTypeSearchResult(string text) {
    var year = text.GetFromRange(text.GetRangeBetween("(", null, ")"));
    var type = text.GetFromRange(text.GetRangeBetween("[", null, "]"));
    var yearInt = int.TryParse(year, out var yi) ? yi : 0;
    return new(yearInt, type);
  }

  public static MovieDetail ParseMovie(string text, DetailId detailId) {
    

    return null;
  }
}