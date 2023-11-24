using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class VideoClipsM : ObservableObject {
  private readonly VideoClipsDataAdapter _da;
  private VideoClipM _currentVideoClip;
  private List<KeyValuePair<string, KeyValuePair<int, string>[]>> _vidThumbsToRebuild;

  public MediaItemM CurrentMediaItem { get; set; }
  public VideoClipM CurrentVideoClip { get => _currentVideoClip; set => SetCurrent(value); }
  public MediaPlayerM MediaPlayerM { get; set; }
  public VideoClipsTreeCategory TreeCategory { get; }
  public Action<string> CreateThumbnail { get; set; }
  public List<KeyValuePair<string, KeyValuePair<int, string>[]>> VidThumbsToRebuild { get => _vidThumbsToRebuild; set { _vidThumbsToRebuild = value; OnPropertyChanged(); } }

  public RelayCommand<bool> SetMarkerCommand { get; }
  public RelayCommand<PlayType> SetPlayTypeCommand { get; }
  public RelayCommand<object> SplitCommand { get; }
  public RelayCommand<int> SeekToPositionCommand { get; }
  public RelayCommand<object> RebuildVideoClipsThumbnailsCommand { get; }

  public VideoClipsM(VideoClipsDataAdapter da) {
    _da = da;
    _da.ItemCreatedEvent += OnItemCreated;
    TreeCategory = new(_da);

    SetMarkerCommand = new(SetMarker, () => CurrentVideoClip != null);
    SetPlayTypeCommand = new(pt => MediaPlayerM.PlayType = pt);

    SplitCommand = new(
      VideoClipSplit,
      () => !string.IsNullOrEmpty(MediaPlayerM?.Source));

    SeekToPositionCommand = new(pos => MediaPlayerM.TimelinePosition = pos);

    RebuildVideoClipsThumbnailsCommand = new(
      () => RebuildVideoClipsThumbnails(Core.MediaItemsM.GetActive()),
      () => Core.MediaItemsM.GetActive().Any());
  }

  private void OnItemCreated(object sender, ObjectEventArgs<VideoClipM> e) {
    CurrentVideoClip = e.Data;
    CurrentMediaItem.HasVideoClips = true;
    TreeCategory.UpdateClipsTitles();
    SetMarker(true);
    TreeCategory.TreeView.ScrollTo(e.Data);
  }

  private void VideoClipSplit() {
    if (CurrentVideoClip?.TimeEnd == 0)
      SetMarker(false);
    else
      TreeCategory.ItemCreate(TreeCategory);
  }

  private void SetCurrent(VideoClipM vc) {
    if (_currentVideoClip != null)
      _currentVideoClip.IsSelected = false;
    _currentVideoClip = vc;
    if (_currentVideoClip != null)
      _currentVideoClip.IsSelected = true;
  }

  public void SetPlayer(MediaPlayerM player) {
    MediaPlayerM = player;
    MediaPlayerM.SelectNextClip = SelectNext;
  }

  public void SetCurrentVideoClip(VideoClipM vc) {
    if (vc == null) return;
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

    _da.IsModified = true;

    MediaPlayerM.ClipTimeStart = vc.TimeStart;
    MediaPlayerM.ClipTimeEnd = vc.TimeEnd;

    if (start) {
      CreateThumbnail(vc.ThumbPath);
      vc.OnPropertyChanged(nameof(vc.ThumbPath));
    }
  }

  public void SetMediaItem(MediaItemM mi) {
    CurrentMediaItem = mi;
    TreeCategory.ReloadClips(mi);
  }

  private void SelectNext(bool inGroup, bool selectFirst) {
    var clip = GetNextClip(inGroup, selectFirst);
    if (clip == null) return;
    SetCurrentVideoClip(clip);
  }

  private VideoClipM GetNextClip(bool inGroup, bool selectFirst) {
    var groups = new List<List<VideoClipM>>();

    groups.AddRange(TreeCategory.Items.OfType<VideoClipsGroupM>()
      .Where(g => g.Items.Count > 0)
      .Select(g => g.Items.Cast<VideoClipM>().ToList()));

    var clips = TreeCategory.Items.OfType<VideoClipM>().ToList();
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

  public void RebuildVideoClipsThumbnails(MediaItemM[] items) {
    var data = new List<KeyValuePair<string, KeyValuePair<int, string>[]>>();
    foreach (var mi in items.Where(x => x.MediaType == MediaType.Video && x.HasVideoClips))
      if (_da.MediaItemVideoClips.TryGetValue(mi, out var miClips)) {
        var clips = miClips
          .Flat()
          .OfType<VideoClipM>()
          .Select(x => new KeyValuePair<int, string>(x.TimeStart, x.ThumbPath))
          .ToArray();

        if (clips.Any())
          data.Add(new(mi.FilePath, clips));
      }

    VidThumbsToRebuild = data;
  }
}