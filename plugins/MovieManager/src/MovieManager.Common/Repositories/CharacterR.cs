using MH.Utils.BaseClasses;
using MovieManager.Common.Models;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|Name|Actor|Movie
/// </summary>
public class CharacterR(CoreR coreR) : TableDataAdapter<CharacterM>(coreR, "Character", 4) {
  public override CharacterM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(CharacterM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.Name,
      item.Actor.GetHashCode().ToString(),
      item.Movie.GetHashCode().ToString());

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Actor = coreR.Actor.GetById(csv[2]);
      item.Movie = coreR.Movie.GetById(csv[3]);
    }
  }

  public CharacterM ItemCreate(string name, ActorM actor, MovieM movie) =>
    ItemCreate(new(GetNextId(), name) { Actor = actor, Movie = movie });
}