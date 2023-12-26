using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoClipsM : ObservableObject {
  private readonly VideoClipsDataAdapter _da;
  private List<VideoClipM> _clipsThumbsToRebuild;
  private List<KeyValuePair<string, KeyValuePair<int, string>[]>> _vidThumbsToRebuild;
  private VideoClipM _current;

  public VideoClipM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
  public List<KeyValuePair<string, KeyValuePair<int, string>[]>> VidThumbsToRebuild { get => _vidThumbsToRebuild; set { _vidThumbsToRebuild = value; OnPropertyChanged(); } }
  
  public Action VidThumbsRebuildFinishedAction => VidThumbsRebuildFinished;

  public RelayCommand<object> RebuildVideoClipsThumbnailsCommand { get; }

  public VideoClipsM(VideoClipsDataAdapter da) {
    _da = da;
    _da.ItemCreatedEvent += OnItemCreated;

    RebuildVideoClipsThumbnailsCommand = new(
      () => RebuildVideoClipsThumbnails(Core.MediaItemsM.GetActive()),
      () => Core.MediaItemsM.GetActive().Any());
  }

  private void OnItemCreated(object sender, ObjectEventArgs<VideoClipM> e) {
    Core.VideosM.Current.HasVideoClips = true;
  }

  public void RebuildVideoClipsThumbnails(MediaItemM[] items) {
    _clipsThumbsToRebuild ??= new();
    var data = new List<KeyValuePair<string, KeyValuePair<int, string>[]>>();
    var vids = items.OfType<VideoM>().Where(x => x.HasVideoClips).ToHashSet();
    var vidsClips = Core.Db.VideoClips.All.Where(x => vids.Contains(x.Video)).GroupBy(x => x.Video);
    
    foreach (var vidClips in vidsClips) {
      _clipsThumbsToRebuild.AddRange(vidClips);
      data.Add(new(vidClips.Key.FilePath,
        vidClips.Select(x => new KeyValuePair<int, string>(x.TimeStart, x.FilePathCache)).ToArray()));
    }

    VidThumbsToRebuild = data;
  }

  private void VidThumbsRebuildFinished() {
    foreach (var vc in _clipsThumbsToRebuild)
      vc.OnPropertyChanged(nameof(vc.FilePathCache));

    _clipsThumbsToRebuild.Clear();
  }
}