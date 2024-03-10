using MH.UI.HelperClasses;
using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Services;
using PictureManager.Domain.ViewModels;
using PictureManager.Domain.ViewModels.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Utils;

public static class VideoThumbsU {
  private static HashSet<MediaItemM> _workingOn;
  private static HashSet<MediaItemM> _todo;

  public static void Create(MediaItemM[] items, bool rebuild = false) {
    _todo ??= [];
    _workingOn ??= [];
    if (_workingOn.Count > 0) {
      foreach (var mi in items) _todo.Add(mi);
      return;
    }

    var videos = GetVideos(items, rebuild);
    if (videos.Length > 0)
      CoreVM.VideoFrameSaver.Save(videos, OnSave, OnError, OnFinished);
  }

  private static VfsVideo[] GetVideos(MediaItemM[] items, bool rebuild) {
    var vids = items.OfType<VideoM>().Concat(items.OfType<VideoItemM>().Select(x => x.Video)).Distinct().ToArray();
    var vidsItems = items.GetVideoItems().GroupBy(x => x.Video);
    var dic = new Dictionary<VideoM, VfsVideo>();

    foreach (var vid in vids) {
      var frames = new List<VfsFrame>();
      AddFrames(vid, frames, rebuild);
      dic.Add(vid, new(vid.FilePath, vid.Orientation.ToAngle(), frames));
    }

    foreach (var vidItems in vidsItems)
      foreach (var vi in vidItems)
        AddFrames(vi, dic[vidItems.Key].Frames, rebuild);

    return dic.Values.Where(x => x.Frames.Any()).ToArray();
  }

  private static void AddFrames(MediaItemM mi, List<VfsFrame> frames, bool rebuild) {
    if (rebuild || !File.Exists(mi.FilePathCache)) {
      _workingOn.Add(mi);
      frames.Add(ToVfsFrame(mi));
    }

    frames.AddRange(ToVfsFrames(mi.GetSegments(), rebuild));
  }

  private static IEnumerable<VfsFrame> ToVfsFrames(IEnumerable<SegmentM> segments, bool rebuild) {
    foreach (var s in segments.Where(s => rebuild || !File.Exists(s.FilePathCache))) {
      _workingOn.Add(s.MediaItem);
      yield return ToVfsFrame(s);
    }
  }

  private static VfsFrame ToVfsFrame(MediaItemM mi) => new(
    mi,
    mi is VideoItemM vi ? vi.TimeStart : 0,
    mi.FilePathCache,
    Core.Settings.MediaItem.ThumbSize,
    Core.Settings.Common.JpegQuality);

  private static VfsFrame ToVfsFrame(SegmentM s) => new(
    s,
    s.MediaItem is VideoItemM vi ? vi.TimeStart : 0,
    s.FilePathCache,
    SegmentS.SegmentSize,
    Core.Settings.Common.JpegQuality,
    (int)s.X, (int)s.Y, (int)s.Size, (int)s.Size);

  private static void OnSave(VfsFrame frame) {
    switch (frame.Source) {
      case MediaItemM mi:
        MediaItemVM.ThumbConverter.IgnoreCache.Add(mi);
        mi.OnPropertyChanged(nameof(mi.FilePathCache));
        break;
      case SegmentM s:
        SegmentVM.ThumbConverter.IgnoreCache.Add(s);
        s.OnPropertyChanged(nameof(s.FilePathCache));
        break;
    }
  }

  private static void OnError(VfsFrame frame, Exception ex) {
    switch (frame.Source) {
      case MediaItemM mi:
        MediaItemVM.ThumbConverter.ErrorCache.Add(mi);
        Log.Error(ex, $"{ex.Message} ({mi.FilePath})");
        break;
      case SegmentM s:
        SegmentVM.ThumbConverter.ErrorCache.Add(s);
        Log.Error(ex, $"{ex.Message} ({s.MediaItem.FilePath})");
        break;
    }
  }

  private static void OnFinished() {
    _workingOn.Clear();
    if (_todo.Count == 0) return;
    var todo = _todo.ToArray();
    _todo.Clear();
    Create(todo);
  }
}