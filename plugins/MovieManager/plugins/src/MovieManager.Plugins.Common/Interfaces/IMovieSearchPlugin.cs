using MovieManager.Plugins.Common.Models;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieSearchPlugin {
  public Task<SearchResult[]> SearchMovie(string query);
}