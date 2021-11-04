using System;
using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|MediaItem|Clips
  /// </summary>
  public class VideoClipsGroupsDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly VideoClipsGroups _model;

    public VideoClipsGroupsDataAdapter(Core core, VideoClipsGroups model) : base("VideoClipsGroups", core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All.Cast<VideoClipsGroup>(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      _model.All.Add(new VideoClipsGroup(int.Parse(props[0]), props[1]) { Csv = props });
    }

    public static string ToCsv(VideoClipsGroup videoClipsGroup) =>
      string.Join("|",
        videoClipsGroup.Id.ToString(),
        videoClipsGroup.Name ?? string.Empty,
        videoClipsGroup.MediaItem.Id.ToString(),
        videoClipsGroup.Clips == null ? string.Empty : string.Join(",", videoClipsGroup.Clips.Select(x => x.Id)));

    public override void LinkReferences() {
      foreach (var vcg in _model.All.Cast<VideoClipsGroup>()) {
        // reference to MediaItem and back reference from MediaItem to VideoClipsGroup
        vcg.MediaItem = _core.MediaItems.AllDic[int.Parse(vcg.Csv[2])];
        vcg.MediaItem.VideoClipsGroupAdd(vcg);

        // reference to VideoClip and back reference from VideoClip to VideoClipsGroup
        if (!string.IsNullOrEmpty(vcg.Csv[3])) {
          var ids = vcg.Csv[3].Split(',');
          vcg.Clips = new(ids.Length);
          foreach (var vcId in ids)
            vcg.MediaItem.VideoClipAdd(_core.VideoClips.AllDic[int.Parse(vcId)], vcg);
        }

        // csv array is not needed any more
        vcg.Csv = null;
      }
    }
  }
}
