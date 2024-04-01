using System.Linq;
using System.Text.Json;
using MovieManager.Plugins.Common.Interfaces;

namespace MovieManager.Plugins.IMDbAPIdev;

public class Core : IPluginCore, IMovieDetailPlugin, IActorDetailPlugin {
  public IMovieDetail GetMovieDetail(string id) {
    var query = Queries.GetTitleById(id, 10);
    var json = Queries.Execute(query).GetProperty("title");
    var movie = json.Deserialize<MovieDetail>();
    movie.Rating = json.GetProperty("rating").GetProperty("aggregate_rating").GetDouble();
    movie.MPAA = json.GetProperty("certificates").EnumerateArray()
      .Select(x => x.GetProperty("rating").GetString()).ToArray();

    return movie;
  }

  public void GetActorById(string id) {
    var query = Queries.GetNameById(id, 5, 20);
    //Queries.Execute(query).ContinueWith(response => { });
  }
}