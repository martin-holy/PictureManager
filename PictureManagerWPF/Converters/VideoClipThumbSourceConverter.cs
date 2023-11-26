using MH.UI.WPF.Converters;
using MH.Utils;
using PictureManager.Domain.Models;
using PictureManager.Domain;
using System.Windows.Media.Imaging;
using System;
using System.IO;
using System.Collections.Generic;

namespace PictureManager.Converters;

public sealed class VideoClipThumbSourceConverter : BaseMarkupExtensionMultiConverter {
  private static readonly HashSet<VideoClipM> _ignoreCache = new();

  public override object Convert(object[] values, object parameter) {
    try {
      if (values is not [_, VideoClipM vc]) return null;

      if (!File.Exists(vc.ThumbPath)) {
        if (!File.Exists(vc.MediaItem.FilePath)) {
          Tasks.RunOnUiThread(() =>
            Core.Db.MediaItems.ItemsDelete(new[] { vc.MediaItem }));
          return null;
        }

        CreateThumbnail(vc);
        return null;
      }

      var src = new BitmapImage();
      src.BeginInit();
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.UriSource = new(vc.ThumbPath);

      if (_ignoreCache.Remove(vc))
        src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

      src.EndInit();

      return src;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  private static void CreateThumbnail(VideoClipM vc) {
    _ignoreCache.Add(vc);
    if (ReferenceEquals(Core.VideoClipsM.CurrentVideoClip, vc)) {
      MH.UI.WPF.Utils.Imaging.CreateThumbnailFromVisual(
        AppCore.FullVideo,
        vc.ThumbPath,
        Core.Settings.ThumbnailSize,
        Core.Settings.JpegQualityLevel);
      vc.OnPropertyChanged(nameof(vc.ThumbPath));
    }
    else
      Core.VideoClipsM.RebuildVideoClipsThumbnails(new[] { vc.MediaItem });
  }
}