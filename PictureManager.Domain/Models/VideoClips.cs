using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class VideoClips : ITable {
    public TableHelper Helper { get; set; }
    public List<VideoClip> All { get; } = new List<VideoClip>();
    public Dictionary<int, VideoClip> AllDic { get; set; } = new Dictionary<int, VideoClip>();

    public void NewFromCsv(string csv) {
      // ID|MediaItem|TimeStart|TimeEnd|Name|Volume|Speed|Rating|Comment|People|Keywords
      var props = csv.Split('|');
      if (props.Length != 11) return;
      AddRecord(
        new VideoClip(int.Parse(props[0]),null) {
          TimeStart = props[2].IntParseOrDefault(0),
          TimeEnd = props[3].IntParseOrDefault(0),
          Name = string.IsNullOrEmpty(props[4]) ? null : props[4],
          Csv = props,
          Volume = props[5].IntParseOrDefault(50) / 100.0,
          Speed = props[6].IntParseOrDefault(10) / 10.0,
          Rating = props[7].IntParseOrDefault(0),
          Comment = string.IsNullOrEmpty(props[8]) ? null : props[8]
        });
    }

    public void LinkReferences() {
      foreach (var vc in All) {
        // reference to MediaItem and back reference from MediaItem to VideoClip without group
        vc.MediaItem = Core.Instance.MediaItems.AllDic[int.Parse(vc.Csv[1])];
        if (vc.Group == null)
          vc.MediaItem.VideoClipAdd(vc, null);

        // reference to People and back reference from Person to VideoClip
        if (!string.IsNullOrEmpty(vc.Csv[9])) {
          var ids = vc.Csv[9].Split(',');
          vc.People = new List<Person>(ids.Length);
          foreach (var personId in ids) {
            var p = Core.Instance.People.AllDic[int.Parse(personId)];
            if (p.VideoClips == null)
              p.VideoClips = new List<VideoClip>();
            p.VideoClips.Add(vc);
            vc.People.Add(p);
          }
        }

        // reference to Keywords and back reference from Keyword to VideoClip
        if (!string.IsNullOrEmpty(vc.Csv[10])) {
          var ids = vc.Csv[10].Split(',');
          vc.Keywords = new List<Keyword>(ids.Length);
          foreach (var keywordId in ids) {
            var k = Core.Instance.Keywords.AllDic[int.Parse(keywordId)];
            if (k.VideoClips == null)
              k.VideoClips = new List<VideoClip>();
            k.VideoClips.Add(vc);
            vc.Keywords.Add(k);
          }
        }

        // csv array is not needed any more
        vc.Csv = null;
      }
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      AllDic.Clear();
      Helper.LoadFromFile();
    }

    private void AddRecord(VideoClip record) {
      All.Add(record);
      AllDic.Add(record.Id, record);
    }

    public Task<VideoClip> CreateVideoClipAsync(MediaItem mediaItem, double volume, double speed) {
      return Core.Instance.RunOnUiThread(() => {
          var vc = new VideoClip(Helper.GetNextId(), mediaItem) {
            Volume = volume,
            Speed = speed
          };

          mediaItem.VideoClipAdd(vc, null);
          All.Add(vc);

          return vc;
        }
      );
    }

    public VideoClip ItemCreate(string name, MediaItem mediaItem, VideoClipsGroup group) {
      var vc = new VideoClip(Helper.GetNextId(), mediaItem) {Name = name};
      vc.MediaItem.VideoClipAdd(vc, group);
      All.Add(vc);

      return vc;
    }

    public void ItemRename(VideoClip vc, string name) {
      vc.Name = name;
      Helper.IsModified = true;
    }

    public void ItemDelete(VideoClip vc) {
      vc.MediaItem.VideoClips?.Remove(vc);
      vc.MediaItem = null;
      
      if (vc.Group != null) {
        vc.Group.Clips.Remove(vc);
        vc.Group = null;
        Core.Instance.VideoClipsGroups.Helper.IsModified = true;
      }

      if (vc.People != null)
        foreach (var p in vc.People) {
          p.VideoClips.Remove(vc);
          if (p.VideoClips.Count == 0)
            p.VideoClips = null;
        }
      
      if (vc.Keywords != null)
        foreach (var k in vc.Keywords) {
          k.VideoClips.Remove(vc);
          if (k.VideoClips.Count == 0)
            k.VideoClips = null;
        }

      All.Remove(vc);
      Helper.IsModified = true;
    }

    public void ItemMove(VideoClip vc, VideoClip dest, bool aboveDest) {
      All.Move(vc, dest, aboveDest);
      
      if (vc.Group == null)
        vc.MediaItem.VideoClips.Move(vc, dest, aboveDest);
      else
        vc.Group.Clips.Move(vc, dest, aboveDest);

      Helper.IsModified = true;
    }

    public void ItemMove(VideoClip vc, VideoClipsGroup dest) {
      if (vc.Group == null)
        vc.MediaItem.VideoClips.Remove(vc);

      vc.Group?.Clips.Remove(vc);
      vc.Group = dest;

      if (dest == null)
        vc.MediaItem.VideoClipAdd(vc, null);
      else
        dest.Clips.Add(vc);

      Core.Instance.VideoClipsGroups.Helper.IsModified = true;

      Helper.IsModified = true;
    }
  }
}
