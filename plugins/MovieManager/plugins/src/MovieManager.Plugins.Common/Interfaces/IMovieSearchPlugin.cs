namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieSearchPlugin {
  public IMovieSearchResult[] SearchMovie(string query);
}