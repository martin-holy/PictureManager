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
    private readonly VideoClipsGroupsM _model;

    public VideoClipsGroupsDataAdapter(Core core, VideoClipsGroupsM model) : base("VideoClipsGroups", core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      _model.All.Add(new(int.Parse(props[0]), props[1]) { Csv = props });
    }

    private static string ToCsv(VideoClipsGroupM videoClipsGroup) =>
      string.Join("|",
        videoClipsGroup.Id.ToString(),
        videoClipsGroup.Name ?? string.Empty,
        videoClipsGroup.MediaItem.Id.ToString(),
        videoClipsGroup.Clips == null ? string.Empty : string.Join(",", videoClipsGroup.Clips.Select(x => x.Id)));

    public override void LinkReferences() {
      foreach (var group in _model.All.Cast<VideoClipsGroupM>()) {
        // reference to MediaItem and back reference from MediaItem to VideoClipsGroup
        VideoClipsGroupsM.SetMediaItem(group, _core.MediaItemsM.AllDic[int.Parse(group.Csv[2])]);

        // reference to VideoClip and back reference from VideoClip to VideoClipsGroup
        if (!string.IsNullOrEmpty(group.Csv[3])) {
          var ids = group.Csv[3].Split(',');
          group.Clips = new(ids.Length);
          foreach (var vcId in ids)
            VideoClipsM.VideoClipAdd(group.MediaItem, _core.VideoClipsM.AllDic[int.Parse(vcId)], group);
        }

        // csv array is not needed any more
        group.Csv = null;
      }
    }
  }
}
