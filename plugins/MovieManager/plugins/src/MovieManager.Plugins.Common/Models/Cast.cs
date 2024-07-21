namespace MovieManager.Plugins.Common.Models;

public class Cast(Actor actor, string[] characters) {
  public Actor Actor { get; } = actor;
  public string[] Characters { get; } = characters;
}