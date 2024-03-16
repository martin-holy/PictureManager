namespace PictureManager.Plugins.Common.Interfaces.Repositories;

public interface IPluginCoreR {
  public IPluginKeywordR Keyword { get; }
  public IPluginPersonR Person { get; }
}