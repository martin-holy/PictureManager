namespace MovieManager.Plugins.Common.Models;

public class DetailId(string id, string name) {
  public string Id { get; } = id;
  public string Name { get; } = name;
}