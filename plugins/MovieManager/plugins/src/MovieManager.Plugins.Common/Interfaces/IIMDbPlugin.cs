namespace MovieManager.Plugins.Common.Interfaces;

public interface IIMDbPlugin : IImportPlugin {
  public string AddImgUrlParams(string url, string urlParams);
}