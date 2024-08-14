using MH.Utils;
using System;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ImageS(ImageR r) {
  public static Func<ImageM, int, bool> WriteMetadata { get; set; } = null!;

  public bool TryWriteMetadata(ImageM img, int quality) {
    try {
      if (!WriteMetadata(img, quality)) throw new("Error writing metadata");
      img.IsOnlyInDb = false;
    }
    catch (Exception ex) {
      Log.Error(ex, $"Metadata will be saved just in database. {img.FilePath}");
      img.IsOnlyInDb = true;
    }

    r.IsModified = true;
    return !img.IsOnlyInDb;
  }
}