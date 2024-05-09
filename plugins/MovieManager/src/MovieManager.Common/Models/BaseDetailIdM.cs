namespace MovieManager.Common.Models;

public abstract class BaseDetailIdM(int id, string detailId, string detailName) {
  public int Id { get; } = id;
  public string DetailId { get; } = detailId;
  public string DetailName { get; } = detailName;

  public override int GetHashCode() => Id;
}