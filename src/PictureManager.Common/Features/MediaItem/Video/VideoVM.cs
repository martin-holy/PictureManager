using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using System;
using System.IO;
using System.Linq;
using PictureManager.Common.Features.Common;
using System.Collections.Generic;

namespace PictureManager.Common.Features.MediaItem.Video;

public sealed class VideoVM : ObservableObject {
  private VideoM? _current;

  public VideoItemCollectionView CurrentVideoItems { get; } = new();
  public VideoM? Current { get => _current; private set { _current = value; OnPropertyChanged(); } }
  public MediaPlayer MediaPlayer { get; } = new();

  public static Func<string, string, object[]?> GetVideoMetadataFunc { get; set; } = null!;

  public VideoVM() {
    MediaPlayer.SelectNextItemAction = CurrentVideoItems.SelectNextOrFirstItem;
    MediaPlayer.GetNewClipFunc = _getNewClip;
    MediaPlayer.GetNewImageFunc = _getNewImage;
    MediaPlayer.OnItemDeleteAction = _onItemDelete;
    MediaPlayer.MarkerSetEvent += _onMarkerSet;
  }

  public void SetCurrent(MediaItemM? item, bool setSource = false) {
    var vid = item as VideoM ?? (item as VideoItemM)?.Video;

    if (vid == null) {
      _setVideoSource(null);
      Current = null;
      CurrentVideoItems.Root.Clear();
      CurrentVideoItems.Selected.DeselectAll();
      return;
    }

    if (ReferenceEquals(Current, vid) && !setSource) return;

    CurrentVideoItems.Selected.DeselectAll();
    Current = vid;
    _setVideoSource(Current);
    _reloadCurrentVideoItems();
    Core.VM.MainWindow.ToolsTabs.Activate(MH.UI.Res.IconMovieClapper, "Video", this);
  }

  private void _reloadCurrentVideoItems() {
    List<VideoItemM> items = [];
    var sortSource = true;

    if (Current != null) {
      if (Core.R.VideoItemsOrder.All.TryGetValue(Current, out var list)) {
        items = list.ToList();
        sortSource = false;
      }
      else
        items = Current.GetVideoItems().ToList();
    }

    var groupByItems = new[] { GroupByItems.GetKeywordsInGroup(items) };
    CurrentVideoItems.Reload(items, GroupMode.ThenByRecursive, groupByItems, true, sortSource);
  }

  private IVideoClip? _getNewClip(int timeStart) =>
    Current == null ? null : Core.R.VideoClip.CustomItemCreate(Current, timeStart);

  private IVideoImage? _getNewImage(int timeStart) =>
    Current == null ? null : Core.R.VideoImage.CustomItemCreate(Current, timeStart);

  private async void _onItemDelete() {
    if (await Core.VM.MediaItem.Delete(CurrentVideoItems.Selected.Items.Cast<MediaItemM>().ToArray()))
      MediaPlayer.SetCurrent(null);
  }

  private void _onMarkerSet(object? sender, Tuple<IVideoItem, bool> e) {
    var item = (VideoItemM)e.Item1;
    Core.R.MediaItem.Modify(item);

    if (!ReferenceEquals(item, CurrentVideoItems.Selected.Items.FirstOrDefault()))
      CurrentVideoItems.Selected.Select(item);

    if (item is VideoClipM && !e.Item2) return; // if !start
    CurrentVideoItems.Insert(item);
    File.Delete(item.FilePathCache);
    item.OnPropertyChanged(nameof(item.FilePathCache));
  }

  private void _setVideoSource(VideoM? vid) {
    if (vid == null) {
      MediaPlayer.Source = string.Empty;
      return;
    }

    var fps = GetVideoMetadataFunc(vid.Folder.FullPath, vid.FileName) is { } data && (double)data[3] > 0
      ? (double)data[3]
      : 30.0;
    var smallChange = Math.Round(1000 / fps, 0);

    MediaPlayer.Source = vid.FilePath;
    MediaPlayer.TimelineSmallChange = smallChange;
  }
}