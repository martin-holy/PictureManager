using MovieManager.Plugins.Common.Interfaces;
using System.Linq;
using System.Text.Json;

namespace MovieManager.Plugins.MediaIMDbCom;

public class Core : IPluginCore, IMovieSearchPlugin, IActorSearchPlugin {
  public IMovieSearchResult[] SearchMovie(string query) {
    var url = $"https://v2.sg.media-imdb.com/suggestion/h/{query.Replace(' ', '+')}.json";
    var jsonText = Common.Core.GetUrlContent(url).Result;
    var jsonData = JsonDocument
      .Parse(jsonText)
      .RootElement
      .GetProperty("d")
      .Deserialize<MovieSearchResult[]>()
      .Cast<IMovieSearchResult>()
      .ToArray();

    return jsonData;
  }

  public IActorSearchResult[] SearchActor(string query) => throw new System.NotImplementedException();
}