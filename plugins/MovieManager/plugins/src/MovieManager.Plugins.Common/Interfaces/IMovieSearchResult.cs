namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieSearchResult {
  public string Name { get; }
  public string Type { get; }
  public int Year { get; }
  public IDetailId DetailId { get; }
  public IImage Image { get; }
}