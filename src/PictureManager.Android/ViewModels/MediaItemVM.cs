using Android.Graphics;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using System;
using System.IO;
using Orientation = MH.Utils.Imaging.Orientation;

namespace PictureManager.Android.ViewModels;

public static class MediaItemVM {
  public static void ReadMetadata(MediaItemMetadata mim, bool gpsOnly = false) {
    if (mim.MediaItem is VideoM) {
      _readVideoMetadata(mim);
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

  private static void _readVideoMetadata(MediaItemMetadata mim) {
    if (ImagingU.GetVideoMetadata(mim.MediaItem.Folder.FullPath, mim.MediaItem.FileName) is not { } data) {
      mim.Success = false;
      Log.Error("Can't read video metadata", mim.MediaItem.FilePath);
      return;
    }

    mim.Height = (int)data[0];
    mim.Width = (int)data[1];
    mim.Orientation = (int)data[2] switch {
      90 => Orientation.Rotate90,
      180 => Orientation.Rotate180,
      270 => Orientation.Rotate270,
      _ => Orientation.Normal,
    };

    mim.Success = true;
  }
}