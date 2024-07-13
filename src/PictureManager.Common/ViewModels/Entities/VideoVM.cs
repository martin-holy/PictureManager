using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using PictureManager.Common.CollectionViews;
using PictureManager.Common.Models.MediaItems;
using System;
using System.IO;
using System.Linq;

namespace PictureManager.Common.ViewModels.Entities;

public sealed class VideoVM : ObservableObject {
  private VideoM? _current;

  public CollectionViewVideoItems CurrentVideoItems { get; } = new();
  public VideoM? Current { get => _current; private set { _current = value; OnPropertyChanged(); } }
  public MediaPlayer MediaPlayer { get; } = new();

  public static Func<string, string, object[]> GetVideoMetadataFunc { get; set; } = null!;

  public VideoVM() {
    MediaPlayer.SelectNextItemAction = CurrentVideoItems.SelectNextOrFirstItem;
    MediaPlayer.GetNewClipFunc = GetNewClip;
    MediaPlayer.GetNewImageFunc = GetNewImage;
    MediaPlayer.OnItemDeleteAction = OnItemDelete;
    MediaPlayer.MarkerSetEvent += OnMarkerSet;
  }

  public void SetCurrent(MediaItemM? item, bool setSource = false) {
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
    Core.VM.MainWindow.ToolsTabs.Activate(MH.UI.Res.IconMovieClapper, "Video", this);
  }

  private void ReloadCurrentVideoItems() {
    var items = Current == null
      ? []
      : Core.R.VideoItemsOrder.All.TryGetValue(Current, out var list)
        ? list.ToList()
        : Current.GetVideoItems().OrderBy(x => x.TimeStart).ToList();
    var groupByItems = new[] { GroupByItems.GetKeywordsInGroup(items) };
    CurrentVideoItems.Reload(items, GroupMode.ThenByRecursive, groupByItems, true);
  }

  private IVideoClip? GetNewClip(int timeStart) =>
    Current == null ? null : Core.R.VideoClip.CustomItemCreate(Current, timeStart);

  private IVideoImage? GetNewImage(int timeStart) =>
    Current == null ? null : Core.R.VideoImage.CustomItemCreate(Current, timeStart);

  private void OnItemDelete() {
    if (Core.VM.MediaItem.Delete(CurrentVideoItems.Selected.Items.Cast<MediaItemM>().ToArray()))
      MediaPlayer.SetCurrent(null);
  }

  private void OnMarkerSet(object? sender, ObjectEventArgs<Tuple<IVideoItem, bool>> e) {
    var item = (VideoItemM)e.Data.Item1;
    Core.R.MediaItem.Modify(item);

    if (!ReferenceEquals(item, CurrentVideoItems.Selected.Items.FirstOrDefault()))
      CurrentVideoItems.Selected.Select(item);

    if (item is VideoClipM && !e.Data.Item2) return; // if !start
    CurrentVideoItems.Insert(item);
    File.Delete(item.FilePathCache);
    item.OnPropertyChanged(nameof(item.FilePathCache));
  }

  private void SetVideoSource(VideoM? vid) {
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