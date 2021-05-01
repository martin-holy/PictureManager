using System.Collections.Generic;
using System.Linq;
using SimpleDB;

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

    public string ToCsv() {
      // ID|MediaItem|TimeStart|TimeEnd|Name|Volume|Speed|Rating|Comment|People|Keywords
      return string.Join("|",
        Id.ToString(),
        MediaItem.Id.ToString(),
        TimeStart.ToString(),
        TimeEnd.ToString(),
        Name ?? string.Empty,
        ((int)(Volume * 100)).ToString(),
        ((int)(Speed * 10)).ToString(),
        Rating == 0 ? string.Empty : Rating.ToString(),
        Comment ?? string.Empty,
        People == null ? string.Empty : string.Join(",", People.Select(x => x.Id)),
        Keywords == null ? string.Empty : string.Join(",", Keywords.Select(x => x.Id)));
    }

    public VideoClip(int id, MediaItem mediaItem) {
      Id = id;
      MediaItem = mediaItem;
    }
  }
}
