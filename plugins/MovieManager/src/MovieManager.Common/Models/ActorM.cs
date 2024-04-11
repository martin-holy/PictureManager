using MH.Utils.BaseClasses;
using PictureManager.Plugins.Common.Interfaces.Models;

namespace MovieManager.Common.Models;

public class ActorM : ListItem {
  public int Id { get; }
  public IPluginHostPersonM Person { get; set; }

  public ActorM(int id, string name) {
    Id = id;
    Name = name;
  }

  public override int GetHashCode() => Id;
}