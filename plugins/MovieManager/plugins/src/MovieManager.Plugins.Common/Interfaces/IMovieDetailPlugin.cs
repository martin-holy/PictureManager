namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieDetailPlugin {
  public IMovieDetail GetMovieDetail(IDetailId id);
}