namespace MovieManager.Common.Models;

public class ActorDetailIdM {
  public int Id { get; }
  public string DetailId { get; }
  public string DetailName { get; }
  public ActorM Actor { get; set; }

  public ActorDetailIdM(int id, string detailId, string detailName) {
    Id = id;
    DetailId = detailId;
    DetailName = detailName;
  }

  public override int GetHashCode() => Id;
}