using System.Xml;
using MH.Utils.BaseClasses;
using PictureManager.Interfaces.Models;

namespace MovieManager.Common.Models;

public class CharacterM : ListItem {
  private ISegmentM _segment;

  public int Id { get; }
  public ActorM Actor { get; set; }
  public MovieM Movie { get; set; }
  public ISegmentM Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }

  public CharacterM(int id, string name) {
    Id = id;
    Name = name;
  }

  public override int GetHashCode() => Id;

  public ISegmentM DisplaySegment {
    get {
      if (Segment != null) return Segment;
      if (Actor?.Person == null) return null;
      if (Movie?.MediaItems == null) return Actor.Person.Segment;

      // TODO
      /*if (Movie.MediaItems.GetSegments().Where(x => ReferenceEquals(x.Person, Actor.Person)).FirstOrDefault() is { } s)
        return s;*/

      return Actor.Person.Segment;
    }
  }
}