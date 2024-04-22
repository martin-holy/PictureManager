using MovieManager.Plugins.Common.Models;
using System;

namespace MovieManager.Plugins.CSFDcz;

public static class Parser {
  public static SearchResult[] ParseSearch(string text) {
    var movies = ExtractText(text, "<section class=\"box main-movies\">", "</section>");
    var serials = ExtractText(text, "<section class=\"box main-series\">", "</section>", movies.Item3);

    return [];
  }

  public static MovieDetail ParseMovie(string text) {
    return null;
  }

  private static Tuple<string, int, int> ExtractText(string text, string from, string to, int start = 0) {
    var startIndex = text.IndexOf(from, start, StringComparison.Ordinal);
    if (startIndex == -1) return null;

    var endIndex = text.IndexOf(to, startIndex, StringComparison.Ordinal);
    if (endIndex == -1) return null;

    startIndex += from.Length;
    var partLength = endIndex - startIndex;
    var substring = text.Substring(startIndex, partLength);

    return new(substring, startIndex, endIndex);
  }
}