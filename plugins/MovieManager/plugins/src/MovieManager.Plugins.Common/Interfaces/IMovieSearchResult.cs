namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieSearchResult {
  public IImage Image { get; }
  public string Id { get; }
  public string Name { get; }
  public string Type { get; }
  public int Year { get; }
}