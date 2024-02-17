using MH.Utils;
using PictureManager.Domain.Database;
using System;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class ImagesM(ImagesDA da) {
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

    da.IsModified = true;
    return !img.IsOnlyInDb;
  }
}