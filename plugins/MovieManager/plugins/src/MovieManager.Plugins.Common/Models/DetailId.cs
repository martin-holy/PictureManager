namespace MovieManager.Plugins.Common.Models;

public class DetailId {
  public string Id { get; set; }
  public string Name { get; set; }

  public DetailId(string id, string name) {
    Id = id;
    Name = name;
  }
}