using MH.Utils.BaseClasses;
using PictureManager.Plugins.Common.Interfaces.Models;

namespace MovieManager.Common.Models;

public class CharacterM : ListItem {
  private IPluginHostSegmentM _segment;

  public int Id { get; }
  public ActorM Actor { get; set; }
  public MovieM Movie { get; set; }
  public IPluginHostSegmentM Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }

  public CharacterM(int id, string name) {
    Id = id;
    Name = name;
  }

  public override int GetHashCode() => Id;
}