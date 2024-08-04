using MovieManager.Plugins.Common.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common.Interfaces;

public interface IImportPlugin {
  public string Name { get; }
  public Task<MovieDetail?> GetMovieDetail(DetailId id, CancellationToken token);
  public Task<SearchResult[]> SearchMovie(string query, CancellationToken token);
}