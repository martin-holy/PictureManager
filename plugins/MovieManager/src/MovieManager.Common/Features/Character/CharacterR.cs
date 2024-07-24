using MH.Utils.BaseClasses;
using MovieManager.Common.Features.Actor;
using MovieManager.Common.Features.Movie;
using PictureManager.Common.Features.Segment;
using System.Linq;
using PM = PictureManager.Common;

namespace MovieManager.Common.Features.Character;

/// <summary>
/// DB fields: Id|Name|Actor|Movie|Segment
/// </summary>
public class CharacterR(CoreR coreR, PM.CoreR pmCoreR) : TableDataAdapter<CharacterM>(coreR, "Characters", 5) {
  public override CharacterM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1], ActorR.Dummy, MovieR.Dummy);

  public override string ToCsv(CharacterM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.Name,
      item.Actor.GetHashCode().ToString(),
      item.Movie.GetHashCode().ToString(),
      item.Segment?.GetHashCode().ToString());

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Actor = coreR.Actor.GetById(csv[2])!;
      item.Movie = coreR.Movie.GetById(csv[3])!;
      item.Segment = pmCoreR.Segment.GetById(csv[4], true);
    }
  }

  public CharacterM ItemCreate(string name, ActorM actor, MovieM movie) =>
    ItemCreate(new(GetNextId(), name, actor, movie));

  public void SetSegment(CharacterM character, SegmentM? segment) {
    if (segment == null) return;
    character.Segment = segment;
    IsModified = true;
  }

  public void OnSegmentDeleted(SegmentM segment) {
    foreach (var character in All.Where(x => ReferenceEquals(x.Segment, segment))) {
      character.Segment = null;
      IsModified = true;
    }
  }
}