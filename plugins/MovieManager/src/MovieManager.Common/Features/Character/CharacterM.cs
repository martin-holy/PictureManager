using MH.Utils.BaseClasses;
using MovieManager.Common.Features.Actor;
using MovieManager.Common.Features.Movie;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using System.Linq;

namespace MovieManager.Common.Features.Character;

public class CharacterM(int id, string name, ActorM actor, MovieM movie) : ListItem(null, name) {
  private SegmentM? _segment;

  public int Id { get; } = id;
  public ActorM Actor { get; set; } = actor;
  public MovieM Movie { get; set; } = movie;
  public SegmentM? Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }

  public override int GetHashCode() => Id;

  public SegmentM? DisplaySegment {
    get {
      if (Segment != null) return Segment;
      if (Actor.Person == null) return null;
      if (Actor.Person.Segment != null) return Actor.Person.Segment;
      if (Movie.MediaItems?.GetSegments().FirstOrDefault(x => ReferenceEquals(x.Person, Actor.Person)) is { } segment)
        return segment;

      return null;
    }
  }
}