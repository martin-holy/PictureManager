using PictureManager.Plugins.Common.Interfaces.Models;

namespace PictureManager.Plugins.Common.Interfaces.Repositories;

public interface IPluginHostCoreR {
  public IPluginHostRepository<IPluginHostKeywordM> Keyword { get; }
  public IPluginHostRepository<IPluginHostMediaItemM> MediaItem { get; }
  public IPluginHostRepository<IPluginHostPersonM> Person { get; }
}