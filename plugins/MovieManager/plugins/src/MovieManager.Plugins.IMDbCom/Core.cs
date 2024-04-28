using MH.Utils;
using MovieManager.Plugins.Common.Interfaces;
using MovieManager.Plugins.Common.Models;
using System;
using System.Threading.Tasks;

namespace MovieManager.Plugins.IMDbCom;

public class Core : IImportPlugin {
  public static readonly string IdName = "IMDb";
  public string Name => "IMDb.com";

  private const string _movieDetailJsonStart = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
  private const string _movieDetailJsonEnd = "</script>";

  public async Task<SearchResult[]> SearchMovie(string query) {
    var url = $"https://v2.sg.media-imdb.com/suggestion/h/{query.Replace(' ', '+')}.json";
    var content = await Common.Core.GetWebPageContent(url);
    return content == null ? [] : Parser.ParseSearch(content);
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
}