using MovieManager.Common.Features.Common;

namespace MovieManager.Common.Features.Actor;

public class ActorDetailIdM(int id, string detailId, string detailName, ActorM actor) : BaseDetailIdM(id, detailId, detailName) {
  public ActorM Actor { get; set; } = actor;
}