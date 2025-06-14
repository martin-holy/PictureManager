using Android.Graphics;
using MH.Utils;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using System;
using System.IO;

namespace PictureManager.MAUI.Droid.ViewModels;

public static class MediaItemVM {
  public static void ReadMetadata(MediaItemMetadata mim, bool gpsOnly = false) {
    if (mim.MediaItem is VideoM) {
      ReadVideoMetadata(mim);
      return;
    }

    try {
      if (mim.MediaItem is ImageM) {
        using Stream srcFileStream = File.OpenRead(mim.MediaItem.FilePath);
        var options = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeStream(srcFileStream, null, options);

        mim.Width = options.OutWidth;
        mim.Height = options.OutHeight;

        // TODO PORT read metadata

        mim.Success = true;
      }
    }
    catch (Exception ex) {
      Log.Error(ex, mim.MediaItem.FilePath);

      // true because only image dimensions are required
      mim.Success = true;
    }
  }

  private static void ReadVideoMetadata(MediaItemMetadata mim) {
    try {
      // TODO PORT read video size and orientation
      var size = new[] { 300, 400, 1 };
      mim.Height = size[0];
      mim.Width = size[1];
      mim.Orientation = size[2] switch {
        90 => Orientation.Rotate90,
        180 => Orientation.Rotate180,
        270 => Orientation.Rotate270,
        _ => Orientation.Normal,
      };

      mim.Success = true;
    }
    catch (Exception ex) {
      Log.Error(ex, mim.MediaItem.FilePath);
      mim.Success = false;
    }
  }
}