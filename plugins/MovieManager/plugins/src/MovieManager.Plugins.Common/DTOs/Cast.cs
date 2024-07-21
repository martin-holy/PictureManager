namespace MovieManager.Plugins.Common.DTOs;

public class Cast(Actor actor, string[] characters) {
  public Actor Actor { get; } = actor;
  public string[] Characters { get; } = characters;
}