using System.Threading.Tasks;

namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieSearchPlugin {
  public Task<IMovieSearchResult[]> SearchMovie(string query);
}