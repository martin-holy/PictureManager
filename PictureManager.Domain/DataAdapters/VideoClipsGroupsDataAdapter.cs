using System;
using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|MediaItem|Clips
  /// </summary>
  public class VideoClipsGroupsDataAdapter : DataAdapter<VideoClipsGroupM> {
    private readonly VideoClipsGroupsM _model;
    private readonly VideoClipsM _videoClipsM;
    private readonly MediaItemsM _mediaItemsM;

    public VideoClipsGroupsDataAdapter(SimpleDB.SimpleDB db, VideoClipsGroupsM model, VideoClipsM vc, MediaItemsM mi)
      : base("VideoClipsGroups", db) {
      _model = model;
      _videoClipsM = vc;
      _mediaItemsM = mi;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() =>
      SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      var group = new VideoClipsGroupM(int.Parse(props[0]), props[1]);
      _model.All.Add(group);
      AllCsv.Add(group, props);
    }

    private static string ToCsv(VideoClipsGroupM vcg) =>
      string.Join("|",
        vcg.Id.ToString(),
        vcg.Name ?? string.Empty,
        vcg.MediaItem.Id.ToString(),
        vcg.Items.Count == 0
          ? string.Empty
          : string.Join(",", vcg.Items.Cast<VideoClipM>().Select(x => x.Id)));

    public override void LinkReferences() {
      foreach (var (group, csv) in AllCsv) {
        group.MediaItem = _mediaItemsM.DataAdapter.AllId[int.Parse(csv[2])];
        group.MediaItem.HasVideoClips = true;
        group.Parent = _videoClipsM;

        if (!string.IsNullOrEmpty(csv[3])) {
          var ids = csv[3].Split(',');

          foreach (var vcId in ids) {
            var vc = _videoClipsM.DataAdapter.AllId[int.Parse(vcId)];
            vc.Parent = group;
            group.Items.Add(vc);
          }
        }

        group.Items.CollectionChanged += _model.GroupItems_CollectionChanged;
      }
    }
  }
}
