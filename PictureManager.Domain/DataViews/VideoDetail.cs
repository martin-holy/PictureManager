using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.DataViews;

public sealed class VideoDetail : ObservableObject {
  private VideoM _current;

  public CollectionViewVideoItems CurrentVideoItems { get; } = new();
  public VideoM Current { get => _current; private set { _current = value; OnPropertyChanged(); } }
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

    CurrentVideoItems.Selected.DeselectAll();
    Current = vid;
    SetVideoSource(Current);
    ReloadCurrentVideoItems();
    Core.VM.MainWindow.ToolsTabs.Activate(Res.IconMovieClapper, "Video", this);
  }

  private void ReloadCurrentVideoItems() {
    var items = Current == null
      ? new()
      : Core.Db.VideoItemsOrder.All.TryGetValue(Current, out var list)
        ? list.ToList()
        : Current.GetVideoItems().OrderBy(x => x.TimeStart).ToList();
    var groupByItems = new[] { GroupByItems.GetKeywordsInGroup(items) };
    CurrentVideoItems.Reload(items, GroupMode.ThenByRecursive, groupByItems, true);
  }

  private IVideoClip GetNewClip(int timeStart) =>
    Core.Db.VideoClips.CustomItemCreate(Current, timeStart);

  private IVideoImage GetNewImage(int timeStart) =>
    Core.Db.VideoImages.CustomItemCreate(Current, timeStart);

  private void OnItemDelete() {
    if (Core.VM.MediaItems.Delete(CurrentVideoItems.Selected.Items.Cast<MediaItemM>().ToArray()))
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