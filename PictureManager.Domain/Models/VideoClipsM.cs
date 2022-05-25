using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsM : TreeCategoryBase {
    private readonly MediaItemsM _mediaItemsM;
    private ITreeItem _scrollToItem;

    public ITreeItem ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public VideoClipsDataAdapter DataAdapter { get; set; }
    public ObservableCollection<ITreeCategory> MediaItemClips { get; }
    public MediaItemM CurrentMediaItem { get; set; }
    public VideoClipM CurrentVideoClip { get; set; }
    public VideoClipsGroupsM GroupsM { get; }

    public event EventHandler<ObjectEventArgs<VideoClipM>> ItemCreatedEventHandler = delegate { };

    public VideoClipsM(MediaItemsM mi) : base(Res.IconMovieClapper, Category.VideoClips, "Clips") {
      _mediaItemsM = mi;
      IsExpanded = true;
      CanMoveItem = true;
      MediaItemClips = new() { this };
      GroupsM = new(this, _mediaItemsM);
    }

    protected override ITreeItem ModelItemCreate(ITreeItem root, string name) {
      var item = new VideoClipM(DataAdapter.GetNextId(), CurrentMediaItem) {
        Parent = root,
        Name = name,
        IsSelected = true
      };

      if (!_mediaItemsM.MediaItemVideoClips.ContainsKey(CurrentMediaItem))
        _mediaItemsM.MediaItemVideoClips.Add(CurrentMediaItem, Items);

      root.Items.Add(item);
      CurrentVideoClip = item;
      CurrentMediaItem.HasVideoClips = true;
      DataAdapter.All.Add(item.Id, item);
      UpdateClipsTitles();
      ItemCreatedEventHandler(this, new(item));

      return item;
    }

    protected override void ModelItemRename(ITreeItem item, string name) {
      item.Name = name;
      DataAdapter.IsModified = true;
      UpdateClipsTitles();
    }

    protected override void ModelItemDelete(ITreeItem item) {
      var vc = (VideoClipM)item;
      File.Delete(vc.ThumbPath);

      vc.MediaItem.HasVideoClips = Items.Count != 0;
      vc.MediaItem = null;
      vc.Parent.Items.Remove(vc);
      vc.Parent = null;
      vc.People = null;
      vc.Keywords = null;

      DataAdapter.All.Remove(vc.Id);
      DataAdapter.IsModified = true;
      UpdateClipsTitles();
    }

    public override void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      null;

    protected override void ModelGroupCreate(ITreeItem root, string name) =>
      GroupsM.ItemCreate(name, CurrentMediaItem);

    protected override void ModelGroupRename(ITreeGroup group, string name) =>
      GroupsM.ItemRename(group, name);

    protected override void ModelGroupDelete(ITreeGroup group) =>
      GroupsM.ItemDelete(group);

    public override void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) =>
      GroupsM.GroupMove(group, dest, aboveDest);

    protected override string ValidateNewGroupName(ITreeItem root, string name) =>
      GroupsM.ItemCanRename(name, CurrentMediaItem)
        ? null
        : $"{name} group already exists!";

    public void SetMarker(VideoClipM clip, bool start, int ms, double volume, double speed) {
      clip.SetMarker(start, ms, volume, speed);
      DataAdapter.IsModified = true;
    }

    public void SetMediaItem(MediaItemM mi) {
      SetCurrentMediaItem(mi);
      UpdateClipsTitles();
      ExpandAll();
    }

    private void SetCurrentMediaItem(MediaItemM mi) {
      CurrentMediaItem = mi;
      Items = mi != null && _mediaItemsM.MediaItemVideoClips.ContainsKey(mi)
        ? _mediaItemsM.MediaItemVideoClips[mi]
        : new();

      OnPropertyChanged(nameof(Items));
    }

    private void UpdateClipsTitles() {
      var nr = 0;
      var clips = Items.OfType<VideoClipsGroupM>()
        .SelectMany(g => g.Items.Select(x => x))
        .Concat(Items.OfType<VideoClipM>())
        .Cast<VideoClipM>();

      foreach (var clip in clips) {
        nr++;
        clip.Title = string.IsNullOrEmpty(clip.Name)
          ? $"Clip #{nr}"
          : clip.Name;
      }
    }

    public void SelectNext(bool inGroup, bool selectFirst) {
      var clip = GetNextClip(inGroup, selectFirst);
      if (clip == null) return;
      if (clip.Equals(CurrentVideoClip))
        clip.IsSelected = false;
      clip.IsSelected = true;
    }

    private VideoClipM GetNextClip(bool inGroup, bool selectFirst) {
      var groups = new List<List<VideoClipM>>();

      groups.AddRange(Items.OfType<VideoClipsGroupM>()
        .Where(g => g.Items.Count > 0)
        .Select(g => g.Items.Cast<VideoClipM>().ToList()));

      var clips = Items.OfType<VideoClipM>().ToList();
      if (clips.Count != 0) 
        groups.Add(clips);

      if (groups.Count == 0)
        return null;

      if (selectFirst)
        return groups[0][0];

      for (var i = 0; i < groups.Count; i++) {
        var group = groups[i];
        var idx = group.IndexOf(CurrentVideoClip);

        if (idx < 0) continue;

        if (idx < group.Count - 1)
          return group[idx + 1];

        return inGroup
          ? group[0]
          : groups[i < groups.Count - 1 ? i + 1 : 0][0];
      }

      return null;
    }
  }
}
