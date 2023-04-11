using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsM : TreeCategoryBase {
    private readonly MediaItemsM _mediaItemsM;
    private ITreeItem _scrollToItem;

    public ITreeItem ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public VideoClipsDataAdapter DataAdapter { get; set; }
    public MediaItemM CurrentMediaItem { get; set; }
    public VideoClipM CurrentVideoClip { get; set; }
    public VideoClipsGroupsM GroupsM { get; }
    public MediaPlayerM MediaPlayerM { get; set; }
    public Action<string> CreateThumbnail { get; set; }
    public readonly HeaderedListItem<object, string> ToolsTabsItem;

    public RelayCommand<bool> SetMarkerCommand { get; }
    public RelayCommand<PlayType> SetPlayTypeCommand { get; }
    public RelayCommand<object> SetCurrentVideoClipCommand { get; }
    public RelayCommand<object> SplitCommand { get; }
    public RelayCommand<object> SaveCommand { get; }
    public RelayCommand<int> SeekToPositionCommand { get; }

    public VideoClipsM(MediaItemsM mi, MediaPlayerM player) : base(Res.IconMovieClapper, Category.VideoClips, "Clips") {
      _mediaItemsM = mi;
      IsExpanded = true;
      CanMoveItem = true;
      GroupsM = new(this, _mediaItemsM);
      MediaPlayerM = player;
      MediaPlayerM.SelectNextClip = SelectNext;
      ToolsTabsItem = new(this, "Clips");

      SetMarkerCommand = new(SetMarker, () => CurrentVideoClip != null);
      SetPlayTypeCommand = new(pt => MediaPlayerM.PlayType = pt);

      SetCurrentVideoClipCommand = new(
        item => SetCurrentVideoClip(item as VideoClipM),
        item => item is VideoClipM);

      SplitCommand = new(
        VideoClipSplit,
        () => !String.IsNullOrEmpty(MediaPlayerM?.Source));

      SaveCommand = new(
        () => {
          DataAdapter.Save();
          GroupsM.DataAdapter.Save();
        },
        () =>
          DataAdapter.IsModified ||
          GroupsM.DataAdapter.IsModified
      );

      SeekToPositionCommand = new(pos => MediaPlayerM.TimelinePosition = pos);
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
      SetMarker(true);
      ScrollToItem = item;

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

    private void VideoClipSplit() {
      if (CurrentVideoClip?.TimeEnd == 0)
        SetMarker(false);
      else
        ItemCreate(this);
    }

    private void SetCurrentVideoClip(VideoClipM vc) {
      CurrentVideoClip = vc;
      MediaPlayerM.ClipTimeStart = vc.TimeStart;
      MediaPlayerM.ClipTimeEnd = vc.TimeEnd;

      if (MediaPlayerM.PlayType != PlayType.Video) {
        MediaPlayerM.Volume = vc.Volume;
        MediaPlayerM.Speed = vc.Speed;
      }

      if (MediaPlayerM.IsPlaying)
        MediaPlayerM.StartClipTimer();
    }

    private void SetMarker(bool start) {
      var vc = CurrentVideoClip;

      vc.SetMarker(
        start,
        (int)Math.Round(MediaPlayerM.TimelinePosition),
        MediaPlayerM.Volume,
        MediaPlayerM.Speed);

      DataAdapter.IsModified = true;

      MediaPlayerM.ClipTimeStart = vc.TimeStart;
      MediaPlayerM.ClipTimeEnd = vc.TimeEnd;

      if (start) {
        CreateThumbnail(vc.ThumbPath);
        vc.OnPropertyChanged(nameof(vc.ThumbPath));
      }
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

    private void SelectNext(bool inGroup, bool selectFirst) {
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
