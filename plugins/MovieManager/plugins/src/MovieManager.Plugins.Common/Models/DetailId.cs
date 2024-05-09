namespace MovieManager.Plugins.Common.Models;

public class DetailId(string id, string name) {
  public string Id { get; } = id ?? string.Empty;
  public string Name { get; } = name ?? string.Empty;
}