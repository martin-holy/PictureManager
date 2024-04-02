using MovieManager.Plugins.Common.Interfaces;

namespace MovieManager.Plugins.Common;

public class DetailId : IDetailId {
  public string Id { get; set; }
  public string Name { get; set; }

  public DetailId(string id, string name) {
    Id = id;
    Name = name;
  }
}