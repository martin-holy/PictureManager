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
}