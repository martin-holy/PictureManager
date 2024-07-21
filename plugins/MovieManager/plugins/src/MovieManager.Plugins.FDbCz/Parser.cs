using MH.Utils;
using MH.Utils.Extensions;
using MovieManager.Plugins.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MovieManager.Plugins.FDbCz;

public static class Parser {
  private static readonly Dictionary<int, string> _genres = new() {
    { 3, "Action" }, { 5, "Adventure" }, { 8, "Animation" }, { 74, "Animation" }, { 37, "Biography" },
    { 76, "Biography" }, { 7, "Comedy" }, { 140, "Comedy" }, { 19, "Comedy" }, { 96, "Comedy" }, { 89, "Comedy" },
    { 119, "Comedy" }, { 41, "Comedy" }, { 18, "Crime" }, { 38, "Documentary" }, { 137, "Documentary" }, { 9, "Drama" },
    { 31, "Family" }, { 12, "Fantasy" }, { 163, "Film-Noir" },  { 16, "History" }, { 103, "History" }, { 15, "Horror" },
    { 13, "Music" }, { 24, "Music" }, { 147, "Mystery" }, { 32, "Romance" }, { 23, "Romance" }, { 135, "Romance" },
    { 33, "Sci-Fi" }, { 77, "Sport" },  { 34, "Thriller" }, { 35, "War" }, { 36, "Western" }, { 169, "Talk-Show" },
    { 175, "Talk-Show" }, { 90, "News" }, { 65, "Music" }, { 113, "History" }, { 172, "Documentary" }, { 133, "Documentary" },
    { 10, "Crime" }, { 11, "Kids" }, { 4, "Erotic" }, { 51, "Kids" }, { 58, "Comedy" }, { 14, "Crime" }, { 1, "Comedy" },
    { 17, "Drama" }, { 46, "Animation" }, { 60, "Fantasy" }, { 117, "Animation" }, { 168, "Biography" }, { 22, "Drama" },
    { 128, "Fantasy" }, { 25, "Documentary" }, { 59, "Music" }, { 57, "Documentary" }, { 68, "Comedy" }, { 26, "Kids" },
    { 27, "Erotic" }, { 100, "Documentary" }, { 173, "Reality-TV" }, { 88, "Comedy" }, { 40, "Action" }, { 45, "Music" },
    { 78, "Drama" }, { 171, "Music" }, { 49, "War" }, { 82, "Music" },
    { 86, "Adaptation" }, { 64, "Agitprop" }, { 63, "Allegorical Images" }, { 43, "Ballad" }, { 2, "Crazy" }, { 102, "Epic" },
    { 123, "Etude" }, { 158, "Experimental" }, { 75, "Film Poem" }, { 127, "Film Essay" }, { 85, "Film Collage" },
    { 101, "Philosophical" }, { 61, "Song Illustration" }, { 138, "Production" }, { 83, "Combined" }, { 166, "Literary" },
    { 21, "Lyrical" }, { 126, "Mystical" }, { 81, "Religious" }, { 104, "Tape" }, { 44, "Parable" }, { 71, "Poetic" },
    { 167, "Poetry" }, { 79, "Political" }, { 66, "Popular Science" }, { 28, "Story" }, { 56, "Promotional" },
    { 136, "Transfer" }, { 29, "Story" }, { 6, "Psychological" }, { 160, "Journalistic" }, { 55, "Advertisement" },
    { 52, "Relaxation" }, { 42, "Retro" }, { 80, "Road Movie" }, { 121, "Saga" }, { 69, "Satire" }, { 84, "Social" },
    { 170, "Competitive" }, { 87, "Social-Critical" }, { 47, "Editing" }, { 73, "Symbolic" }, { 174, "Telenovela" },
    { 125, "Trick" }, { 124, "Artistic" }, { 165, "Entertaining" }, { 92, "Living Pictures" }
  };

  private const string _detailStart = "zakladni_info";
  private const string _castStart = ">hraje:<";
  private const string _classVbox = "class=\"v_box\"";
  private const string _classInfo = "class=\"info\"";
  
  private static readonly StringRange _srSearch = new("id=\"hle_film");
  private static readonly StringRange _srSearchA = new("<a", "</a");
  private static readonly StringRange _srSearchImage = new("rel=\"", "\"");
  private static readonly StringRange _srSearchDetailId = new("href=", "film/", "\"");
  private static readonly StringRange _srSearchName = new(">");
  private static readonly StringRange _srSearchYearAndType = new("<span", ">", "<");
  private static readonly StringRange _srSearchYear = new("(", ")");
  private static readonly StringRange _srSearchType = new("[", "]");
  private static readonly StringRange _srSearchDesc = new("class=\"info\"", ">", "</div");

  private static readonly StringRange _srDetailTitle = new("<a", ">", "</a");
  private static readonly StringRange _srDetailPoster = new("boxPlakaty", "src=\"", "\"");
  private static readonly StringRange _srDetailGenres = new("Žánr:", "clear_text");
  private static readonly StringRange _srDetailGenre = new("<a", ">");
  private static readonly StringRange _srDetailYear = new("Rok:", "right_text\">", "<");
  private static readonly StringRange _srDetailRuntime = new("Délka:", "right_text\">", " ");
  private static readonly StringRange _srDetailIMDbId = new("imdb.com/", "/", "/");
  private static readonly StringRange _srDetailRating = new("hodnoceni\">", ">", "%");
  private static readonly StringRange _srDetailPlot = new("id=\"zbytek\">", "<a");
  private static readonly StringRange _srDetailCasts = new("<table", ">", "</table");
  private static readonly StringRange _srDetailCast = new("<tr", "</tr");
  private static readonly StringRange _srDetailCastImage = new("class=\"photo", "rel=\"", "\"");
  private static readonly StringRange _srDetailCastId = new("class=\"nazev", "lidi/", ".");
  private static readonly StringRange _srDetailCastName = new(">", "<");
  private static readonly StringRange _srDetailCastCharacters = new("role=", ">", "<");

  public static SearchResult[] ParseSearch(string text) {
    var idx = 0;

    return _srSearch.From(text, ref idx)?
      .AsEnumerable(() => ParseSearchResult(text, ref idx))
      .Where(x => x != null)
      .Select(x => x!)
      .ToArray() ?? [];
  }

  private static SearchResult? ParseSearchResult(string text, ref int idx) {
    if (!text.TryIndexOf(_classVbox, ref idx)
        || !text.TryIndexOf(_classInfo, ref idx)) return null;

    var sr = new SearchResult();

    if (_srSearchA.Found(text, idx)) {
      if (_srSearchImage.From(text, _srSearchA)?.AsString(text) is { } imgUrl)
        sr.Image = new(imgUrl);

      if (MovieDetailIdFromUrl(_srSearchDetailId.From(text, _srSearchA)?.AsString(text)) is not { } detailId)
        return null;

      sr.DetailId = detailId;
      sr.Name = _srSearchName.From(text, _srSearchA)?.AsString(text);
    }
    else
      return null;

    if (_srSearchYearAndType.From(text, ref idx) is { } yearType) {
      sr.Year = _srSearchYear.From(text, yearType)?.AsInt32(text) ?? 0;
      sr.Type = _srSearchType.From(text, yearType)?.AsString(text);
    }

    sr.Desc = _srSearchDesc.From(text, idx)?.AsString(text).Replace("<br/>", " ");

    return sr;
  }

  public static async Task<MovieDetail?> ParseMovie(string text, DetailId detailId) {
    var idx = 0;

    if (!text.TryIndexOf(_detailStart, ref idx)
        || _srDetailTitle.From(text, ref idx)?.AsString(text) is not { } title)
      return null;

    var md = new MovieDetail(detailId, title);

    if (_srDetailPoster.From(text, idx)?.AsString(text) is { } posterUrl)
      md.Poster = new(posterUrl);

    md.Genres = ParseGenres(text, _srDetailGenres.From(text, ref idx));
    md.Year = _srDetailYear.From(text, ref idx)?.AsInt32(text) ?? 0;
    md.Runtime = _srDetailRuntime.From(text, ref idx)?.AsInt32(text) ?? 0;
    var imdbId = _srDetailIMDbId.From(text, idx)?.AsString(text);
    md.Rating = ExtractRating(_srDetailRating.From(text, ref idx)?.AsString(text));
    md.Plot = _srDetailPlot.From(text, ref idx)?.AsString(text).Replace("<br />", string.Empty).ReplaceNewLineChars(" ");

    if (text.TryIndexOf(_castStart, ref idx) && _srDetailCasts.Found(text, idx))
      md.Cast = _srDetailCasts
        .AsEnumerable(text, _srDetailCast)
        .Select(x => ParseCast(text, x))
        .Where(x => x != null)
        .Select(x => x!)
        .ToArray();

    if (!string.IsNullOrEmpty(imdbId) && Common.Core.IMDbPlugin != null) {
      var imdbPoster = await Common.Core.IMDbPlugin.GetPoster(imdbId);
      if (imdbPoster != null) md.Poster = imdbPoster;
    }

    // TODO Images <= not worth it, they are to small

    return md;
  }

  private static string[] ParseGenres(string text, StringRange? range) =>
    range?
      .AsEnumerable(text, _srDetailGenre)
      .Select(x => ExtractFirstInt(x?.AsString(text)))
      .Where(x => x != null)
      .Select(x => _genres.TryGetValue(int.Parse(x!), out var genre) ? genre : string.Empty)
      .Where(x => !string.IsNullOrEmpty(x))
      .Distinct()
      .ToArray() ?? [];

  private static double ExtractRating(string? rating) =>
    double.TryParse(rating, CultureInfo.InvariantCulture, out var d) ? Math.Round(d / 10.0, 1) : 0;

  private static Cast? ParseCast(string text, StringRange? range) {
    if (range == null) return null;
    var idx = range.Start;
    var imgUrl = _srDetailCastImage.From(text, ref idx, range.End)?.AsString(text);
    var actorId = _srDetailCastId.From(text, ref idx, range.End)?.AsString(text);
    var actorName = _srDetailCastName.From(text, ref idx, range.End)?.AsString(text).Replace("  ", " ");
    var characters = _srDetailCastCharacters.From(text, ref idx, range.End)?.AsString(text).Split("/") ?? [];

    if (actorId == null || actorName == null || characters.Length == 0) return null;

    var actor = new Actor(new(actorId, Core.IdName), actorName, imgUrl == null ? null : new(imgUrl));

    return new(actor, characters);
  }

  private static string? ExtractFirstInt(string? text) {
    if (text == null) return null;
    var match = Regex.Match(text, @"\d+");
    return match.Success ? match.Value : null;
  }

  public static string MovieDetailIdToUrl(DetailId detailId) =>
    detailId.Id.Replace("_", "/");

  private static DetailId? MovieDetailIdFromUrl(string? url) =>
    string.IsNullOrEmpty(url) ? null : new(url.Replace("/", "_"), Core.IdName);
}