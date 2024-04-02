namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieDetail {
  public IDetailId DetailId { get; }
  public string Title { get; }
  public int Year { get; }
  public int? YearEnd { get; }
  public int Length { get; }
  public double Rating { get; }
  public string[] Genres { get; }
  public string[] MPAA { get; }
  public string Plot { get; }

  public string Type { get; }
  public IImage[] Posters { get; }
  public ICast[] Casts { get; }
}