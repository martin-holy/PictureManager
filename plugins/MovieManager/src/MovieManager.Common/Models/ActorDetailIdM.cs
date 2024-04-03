namespace MovieManager.Common.Models;

public class ActorDetailIdM(int id, string detailId, string detailName) : BaseDetailIdM(id, detailId, detailName) {
  public ActorM Actor { get; set; }
}