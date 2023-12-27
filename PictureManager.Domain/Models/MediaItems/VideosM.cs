using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Database;
using System;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideosM {
  public VideosDA DataAdapter { get; set; }
  public int ModifiedItemsCount => DataAdapter.All.Count(x => x.IsOnlyInDb);
  public CollectionViewVideoItems CurrentVideoItems { get; } = new();
  public VideoM Current { get; private set; }
  public MediaPlayer MediaPlayer { get; } = new();

  public Func<string, string, object[]> GetVideoMetadataFunc { get; set; }

  public VideosM(VideosDA da) {
    DataAdapter = da;
    MediaPlayer.GetNextClipFunc = GetNextClip;
    MediaPlayer.GetNewClipFunc = GetNewClip;
    MediaPlayer.GetNewImageFunc = GetNewImage;
    MediaPlayer.MarkerSetEvent += OnMarkerSet;
  }

  public void ReloadCurrentVideoItems() {
    // TODO mi rewrite check this. don't reload if VideoClip or VideoImage is selected with the same VideoM
    var vid = Core.MediaItemsM.Current as VideoM ?? (Core.MediaItemsM.Current as VideoItemM)?.Video;
    if (ReferenceEquals(Current, vid)) return;

    Current = vid;
    var items = Current == null
      ? new()
      : Core.Db.VideoItemsOrder.All.TryGetValue(Current, out var list)
        ? list
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
    SelectOne(Core.Db.VideoClips.ItemCreate());

  private IVideoImage GetNewImage() =>
    SelectOne(Core.Db.VideoImages.ItemCreate());

  // TODO integrated it to Selected
  private T SelectOne<T>(T item) where T : VideoItemM {
    CurrentVideoItems.Selected.DeselectAll();
    CurrentVideoItems.Selected.Set(item, true);
    return item;
  }

  private void OnMarkerSet(object sender, ObjectEventArgs<Tuple<IVideoItem, bool>> e) {
    switch (e.Data.Item1) {
      case VideoClipM:
        Core.Db.VideoClips.IsModified = true;
        if (!e.Data.Item2) return; // if !start
        break;
      case VideoImageM:
        Core.Db.VideoImages.IsModified = true;
        break;
    }

    var item = (VideoItemM)e.Data.Item1;
    CurrentVideoItems.ReGroupItems(new[] { item }, false);
    // TODO mi rewrite ScrollTo
    File.Delete(item.FilePathCache);
    item.OnPropertyChanged(nameof(item.FilePathCache));
  }

  // TODO mi rewrite
  private VideoClipM GetNextClip(bool inGroup, bool selectFirst) {
    /*var groups = new List<List<VideoClipM>>();

    groups.AddRange(TreeCategory.Items
      .OfType<VideoClipsGroupM>()
      .Where(g => g.Items.Count > 0)
      .Select(g => g.Items.Select(x => x.Data).Cast<VideoClipM>().ToList()));

    var clips = TreeCategory.Items
      .OfType<VideoClipTreeM>()
      .Select(x => x.Data)
      .Cast<VideoClipM>()
      .ToList();
    if (clips.Count != 0)
      groups.Add(clips);

    if (groups.Count == 0)
      return null;

    if (selectFirst)
      return groups[0][0];

    for (var i = 0; i < groups.Count; i++) {
      var group = groups[i];
      var idx = group.IndexOf((VideoClipM)MediaPlayer.CurrentVideoClip);

      if (idx < 0) continue;

      if (idx < group.Count - 1)
        return group[idx + 1];

      return inGroup
        ? group[0]
        : groups[i < groups.Count - 1 ? i + 1 : 0][0];
    }*/

    return null;
  }

  public void SetVideoSource(VideoM vid) {
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
    Core.ToolsTabsM.Activate(Res.IconMovieClapper, "Video", Core.VideosM);
  }
}