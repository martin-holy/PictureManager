using MH.Utils;
using MovieManager.Plugins.Common.Interfaces;
using MovieManager.Plugins.Common.Models;
using System;
using System.Threading.Tasks;

namespace MovieManager.Plugins.IMDbCom;

public class Core : IIMDbPlugin {
  public static readonly string IdName = "IMDb";
  public string Name => "IMDb.com";

  private const string _imgExt = ".jpg";
  private const string _imgUrlParamStart = "_V1_";
  private const string _movieDetailJsonStart = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
  private const string _movieDetailJsonEnd = "</script>";

  public async Task<SearchResult[]> SearchMovie(string query) {
    var url = $"https://v2.sg.media-imdb.com/suggestion/h/{query.Replace(' ', '+')}.json";
    var content = await Common.Core.GetWebPageContent(url);
    if (content == null) return [];

    try {
      return Parser.ParseSearch(content);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return [];
    }
  }

  public async Task<MovieDetail> GetMovieDetail(DetailId id) {
    if (!id.Name.Equals(IdName)) return null;
    var url = $"https://www.imdb.com/title/{id.Id}";
    var content = await Common.Core.GetWebPageContent(url);
    if (content == null) return null;
    if (ExtractMovieDetailJson(content) is not { } jsonText) return null;

    try {
      return Parser.ParseMovie(jsonText);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  private static string ExtractMovieDetailJson(string html) {
    int startIndex = html.IndexOf(_movieDetailJsonStart, StringComparison.Ordinal);
    if (startIndex == -1) return null;

    int endIndex = html.IndexOf(_movieDetailJsonEnd, startIndex, StringComparison.Ordinal);
    if (endIndex == -1) return null;

    startIndex += _movieDetailJsonStart.Length;
    int jsonLength = endIndex - startIndex;

    return html.Substring(startIndex, jsonLength);
  }

  /// <summary>
  /// Adds image url parameters if url contains _V1_
  /// </summary>
  /// <param name="url"></param>
  /// <param name="urlParams">QL is quality, UY is height, UX is width => QL80_UY150</param>
  /// <returns></returns>
  public string AddImgUrlParams(string url, string urlParams) {
    var startIndex = url.LastIndexOf(_imgUrlParamStart, StringComparison.OrdinalIgnoreCase) + _imgUrlParamStart.Length;
    var endIndex = url.LastIndexOf(_imgExt, StringComparison.OrdinalIgnoreCase);

    return startIndex < 0 || endIndex < 0
      ? url
      : url[..startIndex] + urlParams + url[endIndex..];
  }
}