using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.DataViews;

public sealed class VideoDetail {
  public CollectionViewVideoItems CurrentVideoItems { get; } = new();
  public VideoM Current { get; private set; }
  public MediaPlayer MediaPlayer { get; } = new();

  public Func<string, string, object[]> GetVideoMetadataFunc { get; set; }

  public VideoDetail() {
    MediaPlayer.SelectNextItemAction = CurrentVideoItems.SelectNextOrFirstItem;
    MediaPlayer.GetNewClipFunc = GetNewClip;
    MediaPlayer.GetNewImageFunc = GetNewImage;
    MediaPlayer.OnItemDeleteAction = OnItemDelete;
    MediaPlayer.MarkerSetEvent += OnMarkerSet;
  }

  public void SetCurrent(MediaItemM item, bool setSource = false) {
    var vid = item as VideoM ?? (item as VideoItemM)?.Video;

    if (vid == null) {
      SetVideoSource(null);
      Current = null;
      CurrentVideoItems.Root?.Clear();
      CurrentVideoItems.Selected.DeselectAll();
      return;
    }

    if (ReferenceEquals(Current, vid) && !setSource) return;

    Current = vid;
    SetVideoSource(Current);
    ReloadCurrentVideoItems();
    Core.ToolsTabsM.Activate(Res.IconMovieClapper, "Video", this);
  }

  private void ReloadCurrentVideoItems() {
    var items = Current == null
      ? new()
      : Core.Db.VideoItemsOrder.All.TryGetValue(Current, out var list)
        ? list.ToList()
        : Core.Db.VideoClips.All
          .Where(x => ReferenceEquals(x.Video, Current))
          .Cast<VideoItemM>()
          .Concat(Core.Db.VideoImages.All
            .Where(x => ReferenceEquals(x.Video, Current)))
          .OrderBy(x => x.TimeStart)
          .ToList();
    var groupByItems = new[] { GroupByItems.GetKeywordsInGroup(items) };

    CurrentVideoItems.Reload(items, GroupMode.ThenByRecursive, groupByItems, true);
  }

  private IVideoClip GetNewClip() =>
    Core.Db.VideoClips.CustomItemCreate(Current);

  private IVideoImage GetNewImage() =>
    Core.Db.VideoImages.CustomItemCreate(Current);

  private void OnItemDelete() {
    if (Core.MediaItemsM.Delete(CurrentVideoItems.Selected.Items.Cast<MediaItemM>().ToArray()))
      MediaPlayer.SetCurrent(null);
  }

  private void OnMarkerSet(object sender, ObjectEventArgs<Tuple<IVideoItem, bool>> e) {
    var item = (VideoItemM)e.Data.Item1;
    Core.Db.MediaItems.Modify(item);

    if (!ReferenceEquals(item, CurrentVideoItems.Selected.Items.FirstOrDefault()))
      CurrentVideoItems.Selected.Select(item);

    if (item is VideoClipM && !e.Data.Item2) return; // if !start
    CurrentVideoItems.ReGroupItems(new[] { item }, false);
    // TODO mi rewrite ScrollTo
    File.Delete(item.FilePathCache);
    item.OnPropertyChanged(nameof(item.FilePathCache));
  }

  private void SetVideoSource(VideoM vid) {
    if (vid == null) {
      MediaPlayer.IsPlaying = false;
      MediaPlayer.Source = string.Empty;
      return;
    }

    var data = GetVideoMetadataFunc(vid.Folder.FullPath, vid.FileName);
    var fps = (double)data[3] > 0 ? (double)data[3] : 30.0;
    var smallChange = Math.Round(1000 / fps, 0);

    MediaPlayer.Source = vid.FilePath;
    MediaPlayer.TimelineSmallChange = smallChange;
  }
}