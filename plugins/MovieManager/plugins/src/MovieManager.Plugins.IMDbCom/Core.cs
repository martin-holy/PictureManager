using MH.Utils;
using MovieManager.Plugins.Common.Interfaces;
using MovieManager.Plugins.Common.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MovieManager.Plugins.IMDbCom;

public class Core : IIMDbPlugin {
  public static readonly string IdName = "IMDb";
  public string Name => "IMDb.com";

  private const string _imgExt = ".jpg";
  private const string _imgUrlParamStart = "_V1_";
  private static readonly StringRange _srMovieDetailJson = new("__NEXT_DATA__", ">", "</script");

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

  public async Task<MovieDetail?> GetMovieDetail(DetailId id) {
    if (!id.Name.Equals(IdName)) return null;
    var url = $"https://www.imdb.com/title/{id.Id}";
    var content = await Common.Core.GetWebPageContent(url);
    if (content == null) return null;
    if (_srMovieDetailJson.From(content, 0)?.AsString(content) is not { } jsonText) return null;

    try {
      return Parser.ParseMovie(jsonText);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  /// <summary>
  /// Adds image url parameters if url contains _V1_
  /// </summary>
  /// <param name="url"></param>
  /// <param name="urlParams">QL is quality, UY is height, UX is width => QL80_UY150</param>
  /// <returns></returns>
  public static string AddImgUrlParams(string url, string urlParams) {
    var startIndex = url.LastIndexOf(_imgUrlParamStart, StringComparison.OrdinalIgnoreCase) + _imgUrlParamStart.Length;
    var endIndex = url.LastIndexOf(_imgExt, StringComparison.OrdinalIgnoreCase);

    return startIndex < 0 || endIndex < 0
      ? url
      : url[..startIndex] + urlParams + url[endIndex..];
  }

  string IIMDbPlugin.AddImgUrlParams(string url, string urlParams) =>
    AddImgUrlParams(url, urlParams);

  public async Task<Image?> GetPoster(string movieId) {
    var result = await SearchMovie(movieId);
    if (result.FirstOrDefault(x => movieId.Equals(x.DetailId.Id))?.Image is not { } image) return null;
    image.Url = AddImgUrlParams(image.Url, "QL80");
    return image;
  }
}