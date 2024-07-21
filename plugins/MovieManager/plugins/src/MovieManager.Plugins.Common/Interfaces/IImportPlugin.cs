using MovieManager.Plugins.Common.Models;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common.Interfaces;

public interface IImportPlugin {
  public string Name { get; }
  public Task<MovieDetail?> GetMovieDetail(DetailId id);
  public Task<SearchResult[]> SearchMovie(string query);
}