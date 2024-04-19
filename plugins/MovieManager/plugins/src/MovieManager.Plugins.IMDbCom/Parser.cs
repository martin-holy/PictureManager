using MH.Utils.Extensions;
using System.IO;
using System.Text.Json;

namespace MovieManager.Plugins.IMDbCom;

public static class Parser {
  private const string _aboveTheFoldData = "aboveTheFoldData";
  private const string _aggregateRating = "aggregateRating";
  private const string _caption = "caption";
  private const string _certificate = "certificate";
  private const string _endYear = "endYear";
  private const string _genres = "genres";
  private const string _height = "height";
  private const string _id = "id";
  private const string _mainColumnData = "mainColumnData";
  private const string _originalTitleText = "originalTitleText";
  private const string _pageProps = "pageProps";
  private const string _plainText = "plainText";
  private const string _primaryImage = "primaryImage";
  private const string _props = "props";
  private const string _rating = "rating";
  private const string _ratingsSummary = "ratingsSummary";
  private const string _releaseYear = "releaseYear";
  private const string _runtime = "runtime";
  private const string _seconds = "seconds";
  private const string _text = "text";
  private const string _titleText = "titleText";
  private const string _url = "url";
  private const string _width = "width";
  private const string _year = "year";
  private static readonly string[] _captionTextProp = [_caption, _plainText];
  private static readonly string[] _certificateProp = [_certificate, _rating];
  private static readonly string[] _endYearProp = [_releaseYear, _endYear];
  private static readonly string[] _genresProp = [_genres, _genres];
  private static readonly string[] _originalTitleProp = [_originalTitleText, _text];
  private static readonly string[] _ratingProp = [_ratingsSummary, _aggregateRating];
  private static readonly string[] _runtimeProp = [_runtime, _seconds];
  private static readonly string[] _titleProp = [_titleText, _text];
  private static readonly string[] _yearProp = [_releaseYear, _year];

  public static MovieDetail Parse(Stream stream) {
    var json = JsonDocument.Parse(stream);

    if (!json.RootElement.TryGetProperty(_props, out var props)) return null;
    if (!props.TryGetProperty(_pageProps, out var pageProps)) return null;
    if (!pageProps.TryGetProperty(_aboveTheFoldData, out var dataA)) return null;
    if (!pageProps.TryGetProperty(_mainColumnData, out var dataB)) return null;

    var md = new MovieDetail();

    md.Title = dataA.TryGetString(_titleProp);
    md.OriginalTitle = dataA.TryGetString(_originalTitleProp);
    md.Certificate = dataA.TryGetString(_certificateProp);
    md.Year = dataA.TryGetInt32(_yearProp);
    md.YearEnd = dataA.TryGetInt32(_endYearProp);
    md.Runtime = dataA.TryGetInt32(_runtimeProp);
    md.Rating = dataA.TryGetDouble(_ratingProp);

    if (dataA.TryGetProperty(_primaryImage, out var primaryImage))
      md.Poster = ParseImage(primaryImage);

    md.Genres = dataA.TryGetArray(_genresProp, x => x.TryGetString(_text));

    if (md.Runtime != 0) md.Runtime /= 60;

    return md;
  }

  public static Image ParseImage(JsonElement element) =>
    new() {
      Id = element.TryGetString(_id),
      Url = element.TryGetString(_url),
      Height = element.TryGetInt32(_height),
      Width = element.TryGetInt32(_width),
      Desc = element.TryGetString(_captionTextProp)
    };
}