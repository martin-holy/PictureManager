using System.Threading.Tasks;

namespace MovieManager.Plugins.Common.Interfaces;

public interface IMovieDetailPlugin {
  public Task<IMovieDetail> GetMovieDetail(IDetailId id);
}