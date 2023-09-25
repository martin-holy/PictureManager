using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|MediaItem|TimeStart|TimeEnd|Name|Volume|Speed|Rating|Comment|People|Keywords
/// </summary>
public class VideoClipsDataAdapter : TreeDataAdapter<VideoClipM> {
  private readonly VideoClipsM _model;

  public Dictionary<MediaItemM, ExtObservableCollection<ITreeItem>> MediaItemVideoClips { get; } = new();

  public VideoClipsDataAdapter(VideoClipsM model) : base("VideoClips", 11) {
    _model = model;
    Core.Db.ReadyEvent += OnDbReady;
  }

  private void OnDbReady(object sender, EventArgs args) {
    // move all group items to root
    Core.Db.VideoClipsGroups.ItemDeletedEvent += (_, e) => {
      foreach (var item in e.Data.Items.ToArray())
        ItemMove(item, _model.TreeCategory, false);
    };
  }

  public IEnumerable<VideoClipM> GetAll() {
    foreach (var item in MediaItemVideoClips.Values.SelectMany(x => x))
      switch (item) {
        case VideoClipsGroupM vcg:
          foreach (var vc in vcg.Items.Cast<VideoClipM>())
            yield return vc;
          break;

        case VideoClipM vc:
          yield return vc;
          break;
      }
  }

  public override void Save() =>
    SaveDriveRelated(GetAll()
      .GroupBy(x => Tree.GetParentOf<DriveM>(x.MediaItem.Folder))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

  public override VideoClipM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), null) {
      TimeStart = csv[2].IntParseOrDefault(0),
      TimeEnd = csv[3].IntParseOrDefault(0),
      Name = string.IsNullOrEmpty(csv[4]) ? null : csv[4],
      Volume = csv[5].IntParseOrDefault(50) / 100.0,
      Speed = csv[6].IntParseOrDefault(10) / 10.0,
      Rating = csv[7].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[8]) ? null : csv[8]
    };

  public override string ToCsv(VideoClipM vc) =>
    string.Join("|",
      vc.GetHashCode().ToString(),
      vc.MediaItem.GetHashCode().ToString(),
      vc.TimeStart.ToString(),
      vc.TimeEnd.ToString(),
      vc.Name ?? string.Empty,
      ((int)(vc.Volume * 100)).ToString(),
      ((int)(vc.Speed * 10)).ToString(),
      vc.Rating == 0
        ? string.Empty
        : vc.Rating.ToString(),
      vc.Comment ?? string.Empty,
      vc.People == null
        ? string.Empty
        : string.Join(",", vc.People.Select(x => x.GetHashCode().ToString())),
      vc.Keywords == null
        ? string.Empty
        : string.Join(",", vc.Keywords.Select(x => x.GetHashCode().ToString())));

  public override void LinkReferences() {
    foreach (var (vc, csv) in AllCsv) {
      // reference to MediaItem
      vc.MediaItem = Core.Db.MediaItems.AllDict[int.Parse(csv[1])];
      vc.MediaItem.HasVideoClips = true;

      // set parent for clips not in an group
      if (vc.Parent == null) {
        vc.Parent = _model.TreeCategory;

        if (!MediaItemVideoClips.ContainsKey(vc.MediaItem))
          MediaItemVideoClips.Add(vc.MediaItem, new());
        MediaItemVideoClips[vc.MediaItem].Add(vc);
      }

      // reference to People
      vc.People = LinkList(csv[9], Core.Db.People.AllDict);

      // reference to Keywords
      vc.Keywords = LinkList(csv[10], Core.Db.Keywords.AllDict);
    }
  }

  public override VideoClipM ItemCreate(ITreeItem parent, string name) {
    MediaItemVideoClips.TryAdd(_model.CurrentMediaItem, _model.TreeCategory.Items);

    return TreeItemCreate(new(GetNextId(), _model.CurrentMediaItem) {
      Parent = parent,
      Name = name,
      IsSelected = true
    });
  }

  public override void ItemRename(ITreeItem item, string name) {
    item.Name = name;
    IsModified = true;
    RaiseItemRenamed((VideoClipM)item);
  }

  protected override void OnItemDeleted(VideoClipM item) {
    File.Delete(item.ThumbPath);
    item.MediaItem.HasVideoClips = _model.TreeCategory.Items.Count != 0;
    item.MediaItem = null;
    item.Parent.Items.Remove(item);
    item.Parent = null;
    item.People = null;
    item.Keywords = null;
  }

  public override string ValidateNewItemName(ITreeItem parent, string name) =>
    null;
}