namespace MovieManager.Plugins.Common.Interfaces;

public interface ITitleSearchPlugin {
  public ISearchMovie[] SearchMovie(string query);
}