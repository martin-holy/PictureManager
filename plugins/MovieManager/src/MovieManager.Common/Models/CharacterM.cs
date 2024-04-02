using MH.Utils.BaseClasses;

namespace MovieManager.Common.Models;

public class CharacterM : ObservableObject {
  public int Id { get; }
  public string Name { get; set; }
  public ActorM Actor { get; set; }
  public MovieM Movie { get; set; }

  public CharacterM(int id, string name) {
    Id = id;
    Name = name;
  }

  public override int GetHashCode() => Id;
}