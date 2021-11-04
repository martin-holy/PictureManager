using System.Collections.Generic;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsGroup : IRecord {
    public string[] Csv { get; set; }

    // DB Fields
    public int Id { get; }
    public string Name { get; set; }
    public MediaItem MediaItem { get; set; }
    public List<VideoClip> Clips { get; set; } = new();

    public VideoClipsGroup(int id, string name) {
      Id = id;
      Name = name;
    }
  }
}
