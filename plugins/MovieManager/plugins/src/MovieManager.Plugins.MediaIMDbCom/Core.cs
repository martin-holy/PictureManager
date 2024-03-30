using MovieManager.Plugins.Common.Interfaces;
using System.Linq;
using System.Text.Json;

namespace MovieManager.Plugins.MediaIMDbCom;

public class Core : IPluginCore, IMovieSearchPlugin, IActorSearchPlugin {
  public IMovieSearchResult[] SearchMovie(string query) {
    return new[] {
      new MovieSearchResult() { Id = "tt112233", Image = new Image() { Height = 50, Url = "image url 1", Width = 50 }, Name = "Movie name 1", Year = 2020, Type = "movie" },
      new MovieSearchResult() { Id = "tt445566", Image = new Image() { Height = 50, Url = "image url 2", Width = 50 }, Name = "Movie name 2", Year = 2021, Type = "movie" }
    }.Cast<IMovieSearchResult>().ToArray();

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