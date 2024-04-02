namespace MovieManager.Plugins.Common.Interfaces;

public interface ICast {
  public IDetailId DetailId { get; }
  public string Name { get; }
  public string[] Characters { get; }
}