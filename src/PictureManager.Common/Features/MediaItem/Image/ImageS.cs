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

  public static List<Tuple<PersonM?, string?, string[]?>>? GetPeopleSegmentsKeywords(ImageM img) {
    var peopleOnSegments = img.Segments.EmptyIfNull().Select(x => x.Person).Distinct().ToHashSet();

    return img.Segments?
      .Select(x => new Tuple<PersonM?, string?, string[]?>(
        x.Person,
        x.ToMsRect(),
        x.Keywords?.Select(k => k.FullName).ToArray()))
      .Concat(img.People
        .EmptyIfNull()
        .Where(x => !peopleOnSegments.Contains(x))
        .Select(x => new Tuple<PersonM?, string?, string[]?>(x, null, null)))
      .ToList();
  }
}