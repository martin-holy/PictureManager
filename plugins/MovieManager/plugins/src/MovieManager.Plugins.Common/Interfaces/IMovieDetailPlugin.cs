using MovieManager.Plugins.Common.Models;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieDetailPlugin {
  public Task<MovieDetail> GetMovieDetail(DetailId id);
}