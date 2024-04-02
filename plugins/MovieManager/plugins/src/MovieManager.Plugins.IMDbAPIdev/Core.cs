using System.Linq;
using System.Text.Json;
using MovieManager.Plugins.Common;
using MovieManager.Plugins.Common.Interfaces;

namespace MovieManager.Plugins.IMDbAPIdev;

public class Core : IPluginCore, IMovieDetailPlugin, IActorDetailPlugin {
  public const string IdName = "IMDb";

  public IMovieDetail GetMovieDetail(IDetailId id) {
    if (!id.Name.Equals(IdName)) return null;
    var query = Queries.GetTitleById(id.Id, 10);
    var json = Queries.Execute(query).GetProperty("title");
    var movie = json.Deserialize<MovieDetail>();
    movie.DetailId = new DetailId(movie.Id, IdName);
    movie.Rating = json.GetProperty("rating").GetProperty("aggregate_rating").GetDouble();
    movie.MPAA = json.GetProperty("certificates").EnumerateArray()
      .Select(x => x.GetProperty("rating").GetString()).ToArray();

    foreach (var cast in movie.Casts)
      cast.DetailId = new DetailId(cast.CastName.Id, IdName);

    return movie;
  }

  public void GetActorById(string id) {
    var query = Queries.GetNameById(id, 5, 20);
    //Queries.Execute(query).ContinueWith(response => { });
  }
}