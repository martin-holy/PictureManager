namespace PictureManager.Plugins.Common.Interfaces.Repositories;

public interface IPluginHostCoreR {
  public IPluginHostKeywordR Keyword { get; }
  public IPluginHostPersonR Person { get; }
}