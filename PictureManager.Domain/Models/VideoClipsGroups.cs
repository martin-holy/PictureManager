using System.Collections.Generic;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class VideoClipsGroups : ITable {
    public TableHelper Helper { get; set; }
    public List<VideoClipsGroup> All { get; } = new List<VideoClipsGroup>();

    public void NewFromCsv(string csv) {
      // ID|Name|Clips
      var props = csv.Split('|');
      if (props.Length != 3) return;
      All.Add(
        new VideoClipsGroup(int.Parse(props[0]), null) {
          Name = string.IsNullOrEmpty(props[1]) ? null : props[1],
          Csv = props
        });
    }

    public void LinkReferences() {
      foreach (var vcg in All) {
        // reference to VideoClip and back reference from VideoClipsGroup to VideoClip
        if (!string.IsNullOrEmpty(vcg.Csv[2])) {
          var ids = vcg.Csv[2].Split(',');
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

    public void ItemDelete(VideoClipsGroup vcg) {
      foreach (var vc in vcg.Clips)
        Core.Instance.VideoClips.ItemDelete(vc);

      All.Remove(vcg);
      Helper.IsModified = true;
    }
  }
}
