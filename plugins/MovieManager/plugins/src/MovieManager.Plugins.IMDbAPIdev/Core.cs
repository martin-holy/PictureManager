using MovieManager.Plugins.Common.Interfaces;

namespace MovieManager.Plugins.IMDbAPIdev;

public class Core : IPluginCore {
  public void GetMovieById(string id) {
    var query = Queries.GetTitleById(id, 10);
    Queries.Execute(query).ContinueWith(response => { });
  }

  public void GetActorById(string id) {
    var query = Queries.GetNameById(id, 5, 20);
    Queries.Execute(query).ContinueWith(response => { });
  }
}