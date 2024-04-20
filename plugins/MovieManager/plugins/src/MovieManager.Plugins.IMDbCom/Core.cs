using MovieManager.Plugins.Common.Interfaces;
using MovieManager.Plugins.Common.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MovieManager.Plugins.IMDbCom;

public class Core : IPluginCore, IMovieSearchPlugin, IActorSearchPlugin, IMovieDetailPlugin {
  public static readonly string IdName = "IMDb";
  private const string _movieDetailJsonStart = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
  private const string _movieDetailJsonEnd = "</script>";

  public async Task<SearchResult[]> SearchMovie(string query) {
    var url = $"https://v2.sg.media-imdb.com/suggestion/h/{query.Replace(' ', '+')}.json";
    var content = await Common.Core.GetWebPageContent(url);
    return content == null ? [] : Parser.ParseSearch(content);
  }

  public IActorSearchResult[] SearchActor(string query) => throw new System.NotImplementedException();

  public async Task<MovieDetail> GetMovieDetail(DetailId id) {
    //return Parser.ParseMovie(TestJsonImport());

    if (!id.Name.Equals(IdName)) return null;
    var url = $"https://www.imdb.com/title/{id.Id}";
    var content = await Common.Core.GetWebPageContent(url);
    if (content == null) return null;
    var jsonText = ExtractMovieDetailJson(content);
    return Parser.ParseMovie(jsonText);
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

  private string TestJsonImport() {
    using var sr = new StreamReader("d:\\Dev\\PictureManager\\Temp\\TestCompress.json", Encoding.UTF8);
    return sr.ReadToEnd();
  }
}

