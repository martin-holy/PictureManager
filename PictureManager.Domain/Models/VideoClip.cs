using SimpleDB;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class VideoClip : IRecord {
    public string[] Csv { get; set; }

    // DB Fields
    public int Id { get; }
    public MediaItem MediaItem { get; set; }
    public int TimeStart { get; set; }
    public int TimeEnd { get; set; }
    public string Name { get; set; }
    public double Volume { get; set; }
    public double Speed { get; set; }
    public int? Rating { get; set; }
    public string Comment { get; set; }
    public List<Person> People { get; set; }
    public List<Keyword> Keywords { get; set; }
    public VideoClipsGroup Group { get; set; }

    public VideoClip(int id, MediaItem mediaItem) {
      Id = id;
      MediaItem = mediaItem;
    }
  }
}
