using System.Linq;
using MH.Utils;
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

    public VideoClipsGroupsDataAdapter(VideoClipsGroupsM model, VideoClipsM vc, MediaItemsM mi) : base("VideoClipsGroups", 4) {
      _model = model;
      _videoClipsM = vc;
      _mediaItemsM = mi;
    }

    public override void Save() =>
      SaveDriveRelated(_mediaItemsM.MediaItemVideoClips.Values
        .SelectMany(x => x.OfType<VideoClipsGroupM>())
        .GroupBy(x => Tree.GetTopParent(x.MediaItem.Folder))
        .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

    public override VideoClipsGroupM FromCsv(string[] csv) =>
      new(int.Parse(csv[0]), csv[1]);

    public override string ToCsv(VideoClipsGroupM vcg) =>
      string.Join("|",
        vcg.Id.ToString(),
        vcg.Name ?? string.Empty,
        vcg.MediaItem.Id.ToString(),
        vcg.Items.Count == 0
          ? string.Empty
          : string.Join(",", vcg.Items.Cast<VideoClipM>().Select(x => x.Id)));

    public override void LinkReferences() {
      _mediaItemsM.MediaItemVideoClips.Clear();

      foreach (var (group, csv) in AllCsv) {
        group.MediaItem = _mediaItemsM.DataAdapter.All[int.Parse(csv[2])];
        group.MediaItem.HasVideoClips = true;
        group.Parent = _videoClipsM;

        if (!string.IsNullOrEmpty(csv[3])) {
          var ids = csv[3].Split(',');

          foreach (var vcId in ids) {
            var vc = _videoClipsM.DataAdapter.All[int.Parse(vcId)];
            vc.Parent = group;
            group.Items.Add(vc);
          }
        }

        group.Items.CollectionChanged += _model.GroupItems_CollectionChanged;

        if (!_mediaItemsM.MediaItemVideoClips.ContainsKey(group.MediaItem))
          _mediaItemsM.MediaItemVideoClips.Add(group.MediaItem, new());
        _mediaItemsM.MediaItemVideoClips[group.MediaItem].Add(group);
      }
    }
  }
}
