namespace MovieManager.Plugins.Common.DTOs;

public class DetailId(string id, string name) {
  public string Id { get; } = id;
  public string Name { get; } = name;
}