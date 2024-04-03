namespace MovieManager.Common.Models;

public abstract class BaseDetailIdM {
  public int Id { get; }
  public string DetailId { get; }
  public string DetailName { get; }

  protected BaseDetailIdM(int id, string detailId, string detailName) {
    Id = id;
    DetailId = detailId;
    DetailName = detailName;
  }

  public override int GetHashCode() => Id;
}