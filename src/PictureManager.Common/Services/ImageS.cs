using MH.Utils;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.Repositories;
using System;

namespace PictureManager.Common.Services;

public sealed class ImageS(ImageR r) {
  public static Func<ImageM, bool> WriteMetadata { get; set; }

  public bool TryWriteMetadata(ImageM img) {
    try {
      if (!WriteMetadata(img)) throw new("Error writing metadata");
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