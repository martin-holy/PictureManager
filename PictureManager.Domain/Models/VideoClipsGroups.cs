using System;
using System.Collections.Generic;
using System.Linq;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class VideoClipsGroups : ITable {
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new List<IRecord>();

    public void NewFromCsv(string csv) {
      // ID|Name|MediaItem|Clips
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      All.Add(new VideoClipsGroup(int.Parse(props[0]), props[1]) {Csv = props});
    }

    public void LinkReferences() {
      foreach (var vcg in All.Cast<VideoClipsGroup>()) {
        // reference to MediaItem and back reference from MediaItem to VideoClipsGroup
        vcg.MediaItem = Core.Instance.MediaItems.AllDic[int.Parse(vcg.Csv[2])];
        vcg.MediaItem.VideoClipsGroupAdd(vcg);

        // reference to VideoClip and back reference from VideoClip to VideoClipsGroup
        if (!string.IsNullOrEmpty(vcg.Csv[3])) {
          var ids = vcg.Csv[3].Split(',');
          vcg.Clips = new List<VideoClip>(ids.Length);
          foreach (var vcId in ids)
            vcg.MediaItem.VideoClipAdd(Core.Instance.VideoClips.AllDic[int.Parse(vcId)], vcg);
        }

        // csv array is not needed any more
        vcg.Csv = null;
      }
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public VideoClipsGroup ItemCreate(string name, MediaItem mediaItem) {
      var vcg = new VideoClipsGroup(Helper.GetNextId(), name) {MediaItem = mediaItem};
      vcg.MediaItem.VideoClipsGroupAdd(vcg);
      All.Add(vcg);

      return vcg;
    }

    public void ItemRename(VideoClipsGroup vcg, string name) {
      vcg.Name = name;
      Core.Instance.Sdb.SetModified<VideoClipsGroups>();
    }

    public void ItemDelete(VideoClipsGroup vcg) {
      vcg.MediaItem.VideoClipsGroups.Remove(vcg);
      All.Remove(vcg);
      Core.Instance.Sdb.SetModified<VideoClipsGroups>();
    }

    public void GroupMove(VideoClipsGroup group, VideoClipsGroup dest, bool aboveDest) {
      All.Move(group, dest, aboveDest);
      group.MediaItem.VideoClipsGroups.Move(group, dest, aboveDest);
      Core.Instance.Sdb.SetModified<VideoClipsGroups>();
    }
  }
}
