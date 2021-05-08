using System.Collections.Generic;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class VideoClipsGroups : ITable {
    public TableHelper Helper { get; set; }
    public List<VideoClipsGroup> All { get; } = new List<VideoClipsGroup>();

    public void NewFromCsv(string csv) {
      // ID|Name|MediaItem|Clips
      var props = csv.Split('|');
      if (props.Length != 4) return;
      All.Add(new VideoClipsGroup(int.Parse(props[0]), props[1]) {Csv = props});
    }

    public void LinkReferences() {
      foreach (var vcg in All) {
        // reference to MediaItem and back reference from MediaItem to VideoClipsGroup
        vcg.MediaItem = Core.Instance.MediaItems.AllDic[int.Parse(vcg.Csv[2])];
        vcg.MediaItem.VideoClipsGroupAdd(vcg);

        // reference to VideoClip and back reference from VideoClip to VideoClipsGroup
        if (!string.IsNullOrEmpty(vcg.Csv[3])) {
          var ids = vcg.Csv[3].Split(',');
          vcg.Clips = new List<VideoClip>(ids.Length);
          foreach (var vcId in ids) {
            var vc = Core.Instance.VideoClips.AllDic[int.Parse(vcId)];
            vc.Group = vcg;
            vcg.Clips.Add(vc);
          }
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
      SaveToFile();
    }

    public void ItemDelete(VideoClipsGroup vcg) {
      foreach (var vc in vcg.Clips)
        Core.Instance.VideoClips.ItemDelete(vc);

      vcg.MediaItem.VideoClipsGroups.Remove(vcg);

      All.Remove(vcg);
      Helper.IsModified = true;
    }

    public void GroupMove(VideoClipsGroup group, VideoClipsGroup dest, bool aboveDest) {
      All.Move(group, dest, aboveDest);
      group.MediaItem.VideoClipsGroups.Move(group, dest, aboveDest);
      Helper.IsModified = true;
    }
  }
}
