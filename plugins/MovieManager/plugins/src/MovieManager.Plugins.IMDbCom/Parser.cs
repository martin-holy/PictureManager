using MH.Utils.Extensions;
using MovieManager.Plugins.Common.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace MovieManager.Plugins.IMDbCom;

public static class Parser {
  private const string _imgQuality = "QL80";
  private const string _searchPosterUrlParams = "QL80_UY150";
  private const string _aboveTheFoldData = "aboveTheFoldData";
  private const string _aggregateRating = "aggregateRating";
  private const string _caption = "caption";
  private const string _cast = "cast";
  private const string _certificate = "certificate";
  private const string _d = "d";
  private const string _edges = "edges";
  private const string _endYear = "endYear";
  private const string _genres = "genres";
  private const string _height = "height";
  private const string _characters = "characters";
  private const string _i = "i";
  private const string _id = "id";
  private const string _imageUrl = "imageUrl";
  private const string _l = "l";
  private const string _mainColumnData = "mainColumnData";
  private const string _name = "name";
  private const string _nameText = "nameText";
  private const string _node = "node";
  private const string _originalTitleText = "originalTitleText";
  private const string _pageProps = "pageProps";
  private const string _plainText = "plainText";
  private const string _plot = "plot";
  private const string _plotText = "plotText";
  private const string _primaryImage = "primaryImage";
  private const string _props = "props";
  private const string _q = "q";
  private const string _qid = "qid";
  private const string _rating = "rating";
  private const string _ratingsSummary = "ratingsSummary";
  private const string _releaseYear = "releaseYear";
  private const string _runtime = "runtime";
  private const string _s = "s";
  private const string _seconds = "seconds";
  private const string _text = "text";
  //private const string _titleMainImages = "titleMainImages";
  private const string _titleText = "titleText";
  private const string _titleType = "titleType";
  private const string _url = "url";
  private const string _width = "width";
  private const string _y = "y";
  private const string _year = "year";

  private static readonly Dictionary<string, string> _searchResultTypes = new() {
    { "feature", "Movie" },
    { "movie", "Movie" },
    { "video", "Video" },
    { "TV mini-series", "TV mini-series" },
    { "tvMiniSeries", "TV mini-series" },
    { "TV series", "TV series"},
    { "tvSeries", "TV series" },
    { "TV movie", "TV movie" },
    { "tvMovie", "TV movie" },
    { "musicVideo", "Music Video" },
    { "short", "Short" }
  };

  public static SearchResult[] ParseSearch(string text) {
    var json = JsonDocument.Parse(text);
    return json.RootElement.TryGetArray(_d, ParseSearch);
  }

  private static SearchResult ParseSearch(JsonElement element) =>
    new() {
      DetailId = new(element.TryGetString(_id), Core.IdName),
      Name = element.TryGetString(_l),
      Year = element.TryGetInt32(_y),
      Type = ParseSearchType(element),
      Desc = element.TryGetString(_s),
      Image = element.TryGetObject(_i, elm => ParseImage(elm, _searchPosterUrlParams))
    };

  private static string ParseSearchType(JsonElement element) {
    var q = element.TryGetString(_q) ?? string.Empty;
    var qid = element.TryGetString(_qid) ?? string.Empty;

    if (string.IsNullOrEmpty(q) && string.IsNullOrEmpty(qid)) return string.Empty;
    if (!string.IsNullOrEmpty(q) && _searchResultTypes.TryGetValue(q, out var qType)) return qType;
    if (!string.IsNullOrEmpty(qid) && _searchResultTypes.TryGetValue(qid, out var qidType)) return qidType;

    return string.Join(' ', q, qid);
  }

  private static Image ParseImage(JsonElement element, string urlParams) {
    var img = ParseImage(element);
    img.Url = Common.Core.IMDbPlugin.AddImgUrlParams(img.Url, urlParams);
    return img;
  }

  public static MovieDetail ParseMovie(string text) {
    var json = JsonDocument.Parse(text);

    if (!json.RootElement.TryGetProperty(_props, out var props)
        || !props.TryGetProperty(_pageProps, out var pageProps)
        || !pageProps.TryGetProperty(_aboveTheFoldData, out var dataA)
        || !pageProps.TryGetProperty(_mainColumnData, out var dataB)) return null;

    // Images are disabled because of poor content. Random screenshots or not from movie it self.
    var md = new MovieDetail {
      DetailId = new (dataA.TryGetString(_id), Core.IdName),
      Type = dataA.TryGetString(_titleType, _text),
      Title = dataA.TryGetString(_titleText, _text),
      OriginalTitle = dataA.TryGetString(_originalTitleText, _text),
      MPAA = dataA.TryGetString(_certificate, _rating),
      Year = dataA.TryGetInt32(_releaseYear, _year),
      YearEnd = dataA.TryGetInt32(_releaseYear, _endYear),
      Runtime = dataA.TryGetInt32(_runtime, _seconds),
      Rating = dataA.TryGetDouble(_ratingsSummary, _aggregateRating),
      Poster = dataA.TryGetObject(_primaryImage, ParseImage),
      Genres = dataA.TryGetArray(_genres, _genres, x => x.TryGetString(_text)),
      Plot = dataA.TryGetString(_plot, _plotText, _plainText)?.ReplaceNewLineChars(" "),
      //Images = dataB.TryGetArray(_titleMainImages, _edges, x => x.TryGetObject(_node, ParseImage)),
      Cast = dataB.TryGetArray(_cast, _edges, x => x.TryGetObject(_node, ParseCast))
    };

    if (md.Runtime != 0) md.Runtime /= 60;

    return md;
  }

  private static Image ParseImage(JsonElement element) =>
    new() {
      Id = element.TryGetString(_id),
      Url = Common.Core.IMDbPlugin.AddImgUrlParams(
        element.TryGetString(_url) ?? element.TryGetString(_imageUrl), _imgQuality),
      Height = element.TryGetInt32(_height),
      Width = element.TryGetInt32(_width),
      Desc = element.TryGetString(_caption, _plainText)
    };

  private static Cast ParseCast(JsonElement element) =>
    new() {
      Actor = new() {
        DetailId = new(element.TryGetString(_name, _id), Core.IdName),
        Name = element.TryGetString(_name, _nameText, _text),
        Image = element.TryGetObject(_name, _primaryImage, ParseImage)
      },
      Characters = element.TryGetArray(_characters, x => x.TryGetString(_name))
    };
}