namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieDetail {
  public string Id { get; }
  public string Title { get; }
  public int Year { get; }
  public int Length { get; }
  public string Type { get; }
  public bool IsAdult { get; }
  public int? YearEnd { get; }
  public string Plot { get; }
  public double Rating { get; }
  public string[] Genres { get; }
  public IImage[] Posters { get; }
  public string[] MPAA { get; }
  public ICast[] Casts { get; }
}