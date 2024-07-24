using MH.Utils.BaseClasses;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;

namespace MovieManager.Common.Features.Actor;

public class ActorM : ListItem {
  public int Id { get; }
  public PersonM? Person { get; set; }
  public ActorDetailIdM DetailId { get; set; } = null!;
  public MediaItemM? Image { get; set; }

  public ActorM(int id, string name) : base(null, name) {
    Id = id;
  }

  public override int GetHashCode() => Id;
}