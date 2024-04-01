namespace MovieManager.Plugins.Common.Interfaces;

public interface ICast {
  public string Id { get; }
  public string Name { get; }
  public string[] Characters { get; }
}