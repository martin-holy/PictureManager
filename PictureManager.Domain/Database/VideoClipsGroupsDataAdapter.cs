using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Name|MediaItem|Clips
/// </summary>
public class VideoClipsGroupsDataAdapter : TreeDataAdapter<VideoClipsGroupM> {
  private readonly Db _db;
  private readonly VideoClipsM _model;

  public VideoClipsGroupsDataAdapter(Db db, VideoClipsM model) : base("VideoClipsGroups", 4) {
    _db = db;
    _model = model;
    _model.TreeCategory.SetGroupDataAdapter(this);
  }

  public override void Save() =>
    SaveDriveRelated(_db.VideoClips.MediaItemVideoClips.Values
      .SelectMany(x => x.OfType<VideoClipsGroupM>())
      .GroupBy(x => Tree.GetParentOf<DriveM>(x.MediaItem.Folder))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

  public override VideoClipsGroupM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(VideoClipsGroupM vcg) =>
    string.Join("|",
      vcg.GetHashCode().ToString(),
      vcg.Name ?? string.Empty,
      vcg.MediaItem.GetHashCode().ToString(),
      vcg.Items.Count == 0
        ? string.Empty
        : string.Join(",", vcg.Items.Select(x => x.GetHashCode().ToString())));

  public override void LinkReferences() {
    var mivc = _db.VideoClips.MediaItemVideoClips;
    mivc.Clear();

    foreach (var (group, csv) in AllCsv) {
      group.MediaItem = _db.MediaItems.AllDict[int.Parse(csv[2])];
      group.MediaItem.HasVideoClips = true;
      group.Parent = _model.TreeCategory;

      if (!string.IsNullOrEmpty(csv[3])) {
        var ids = csv[3].Split(',');

        foreach (var vcId in ids) {
          var vc = _db.VideoClips.AllDict[int.Parse(vcId)];
          vc.Parent = group;
          group.Items.Add(vc);
        }
      }

      group.Items.CollectionChanged += GroupItems_CollectionChanged;

      if (!mivc.ContainsKey(group.MediaItem))
        mivc.Add(group.MediaItem, new());
      mivc[group.MediaItem].Add(group);
    }
  }

  public override VideoClipsGroupM ItemCreate(ITreeItem parent, string name) =>
    TreeItemCreate(new(GetNextId(), name) { Parent = parent });

  protected override void OnItemCreated(VideoClipsGroupM item) {
    var mi = _model.CurrentMediaItem;
    mi.HasVideoClips = true;
    item.MediaItem = mi;
    item.Items.CollectionChanged += GroupItems_CollectionChanged;
    _db.VideoClips.MediaItemVideoClips.TryAdd(mi, item.Parent.Items);
  }

  public override string ValidateNewItemName(ITreeItem parent, string name) =>
    All.Any(x => x.MediaItem.Equals(_model.CurrentMediaItem) && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
      ? $"{name} group already exists!"
      : null;

  private void GroupItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
    IsModified = true;
  }

  protected override void OnItemDeleted(VideoClipsGroupM item) {
    item.Parent.Items.Remove(item);
    item.MediaItem.HasVideoClips = item.Parent.Items.Count != 0;
  }
}