using MovieManager.Plugins.Common.Models;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common.Interfaces;

public interface IIMDbPlugin : IImportPlugin {
  public string AddImgUrlParams(string url, string urlParams);
  public Task<Image?> GetPoster(string movieId);
}