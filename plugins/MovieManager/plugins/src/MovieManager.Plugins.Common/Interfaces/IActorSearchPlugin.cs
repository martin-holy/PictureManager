namespace MovieManager.Plugins.Common.Interfaces;

public interface IActorSearchPlugin {
  public IActorSearchResult[] SearchActor(string query);
}