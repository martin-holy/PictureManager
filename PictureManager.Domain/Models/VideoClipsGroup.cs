using SimpleDB;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsGroup : IRecord {
    public string[] Csv { get; set; }

    // DB Fields
    public int Id { get; }
    public string Name { get; set; }
    public MediaItem MediaItem { get; set; }
    public List<VideoClip> Clips { get; set; } = new();

    // ID|Name|MediaItem|Clips
    public string ToCsv() =>
      string.Join("|",
        Id.ToString(),
        Name ?? string.Empty,
        MediaItem.Id.ToString(),
        Clips == null ? string.Empty : string.Join(",", Clips.Select(x => x.Id)));

    public VideoClipsGroup(int id, string name) {
      Id = id;
      Name = name;
    }
  }
}
