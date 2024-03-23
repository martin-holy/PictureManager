using PictureManager.Plugins.Common.Interfaces.Models;

namespace PictureManager.Plugins.Common.Interfaces.Repositories;

public interface IPluginHostCoreR {
  public IPluginHostKeywordR Keyword { get; }
  public IPluginHostR<IPluginHostMediaItemM> MediaItem { get; }
  public IPluginHostR<IPluginHostPersonM> Person { get; }
}