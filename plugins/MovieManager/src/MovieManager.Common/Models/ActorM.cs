using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;

namespace MovieManager.Common.Models;

public class ActorM : ListItem {
  public int Id { get; }
  public PersonM Person { get; set; }
  public ActorDetailIdM DetailId { get; set; }
  public MediaItemM Image { get; set; }

  public ActorM(int id, string name) {
    Id = id;
    Name = name;
  }

  public override int GetHashCode() => Id;
}