using MovieManager.Plugins.Common.Interfaces;
using System.Linq;
using System.Text.Json;

namespace MovieManager.Plugins.MediaIMDbCom;

public class Core : IPluginCore, ITitleSearchPlugin {
  public ISearchMovie[] SearchMovie(string query) {
    var url = $"https://v2.sg.media-imdb.com/suggestion/h/{query.Replace(' ', '+')}.json";
    var jsonText = Common.Core.GetUrlContent(url).Result;
    var jsonData = JsonDocument
      .Parse(jsonText)
      .RootElement
      .GetProperty("d")
      .Deserialize<Movie[]>()
      .Cast<ISearchMovie>()
      .ToArray();

    return jsonData;
  }
}