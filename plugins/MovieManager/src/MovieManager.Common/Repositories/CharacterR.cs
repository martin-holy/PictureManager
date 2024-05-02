using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Repositories;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|Name|Actor|Movie|Segment
/// </summary>
public class CharacterR(CoreR coreR, ICoreR phCoreR) : TableDataAdapter<CharacterM>(coreR, "Characters", 5) {
  public override CharacterM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(CharacterM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.Name,
      item.Actor.GetHashCode().ToString(),
      item.Movie.GetHashCode().ToString(),
      item.Segment?.GetHashCode().ToString());

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Actor = coreR.Actor.GetById(csv[2]);
      item.Movie = coreR.Movie.GetById(csv[3]);
      item.Segment = phCoreR.Segment.GetById(csv[4], true);
    }
  }

  public CharacterM ItemCreate(string name, ActorM actor, MovieM movie) =>
    ItemCreate(new(GetNextId(), name) { Actor = actor, Movie = movie });

  public void SetSegment(CharacterM character, ISegmentM segment) {
    if (character == null | segment == null) return;
    character.Segment = segment;
    IsModified = true;
  }
}