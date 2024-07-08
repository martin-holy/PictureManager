using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using System.Linq;

namespace MovieManager.Common.Models;

public class CharacterM : ListItem {
  private SegmentM _segment;

  public int Id { get; }
  public ActorM Actor { get; set; }
  public MovieM Movie { get; set; }
  public SegmentM Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }

  public CharacterM(int id, string name) : base(null, name) {
    Id = id;
  }

  public override int GetHashCode() => Id;

  public SegmentM DisplaySegment {
    get {
      if (Segment != null) return Segment;
      if (Actor?.Person == null) return null;
      if (Movie?.MediaItems == null) return Actor.Person.Segment;
      if (Movie.MediaItems.GetSegments().FirstOrDefault(x => ReferenceEquals(x.Person, Actor.Person)) is { } segment)
        return segment;

      return Actor.Person.Segment;
    }
  }
}