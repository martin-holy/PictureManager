using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MovieManager.Plugins.Common;
using MovieManager.Plugins.Common.Interfaces;

namespace MovieManager.Plugins.IMDbCom;

public class Core : IPluginCore, IMovieSearchPlugin, IActorSearchPlugin, IMovieDetailPlugin {
  public const string IdName = "IMDb";
  private const string _movieDetailJsonStart = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
  private const string _movieDetailJsonEnd = "</script>";

  public async Task<IMovieSearchResult[]> SearchMovie(string query) {
    var url = $"https://v2.sg.media-imdb.com/suggestion/h/{query.Replace(' ', '+')}.json";
    var jsonText = await Common.Core.GetWebPageContent(url);
    var jsonData = JsonDocument
      .Parse(jsonText)
      .RootElement
      .GetProperty("d")
      .Deserialize<MovieSearchResult[]>()
      .Select(x => {
        x.DetailId = new DetailId(x.Id, "IMDb");
        return x;
      })
      .Cast<IMovieSearchResult>()
      .ToArray();

    return jsonData;
  }

  public IActorSearchResult[] SearchActor(string query) => throw new System.NotImplementedException();

  public async Task<IMovieDetail> GetMovieDetail(IDetailId id) {
    TestJsonImport();
    return null;

    if (!id.Name.Equals(IdName)) return null;
    var url = $"https://www.imdb.com/title/{id.Id}";
    var content = await Common.Core.GetWebPageContent(url);
    var json = ExtractMovieDetailJson(content);

    /*using var sw = new StreamWriter("d:\\Dev\\PictureManager\\Temp\\TestCompress.html", false, Encoding.UTF8, 65536);
    sw.Write(content);
    using var sw2 = new StreamWriter("d:\\Dev\\PictureManager\\Temp\\TestCompress.json", false, Encoding.UTF8, 65536);
    sw2.Write(json);*/

    return null;
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

  private void TestJsonImport() {
    using var sr = new StreamReader("d:\\Dev\\PictureManager\\Temp\\TestCompress.json", Encoding.UTF8);
    
    Parser.Parse(sr.BaseStream);
  }
}

