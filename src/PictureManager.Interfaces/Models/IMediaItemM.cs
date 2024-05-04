using MH.Utils.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Interfaces.Models;

public interface IMediaItemM {
  public int Id { get; }
  public int ThumbWidth { get; }
  public int ThumbHeight { get; }
  public void SetThumbSize(bool reload = false);
  public IEnumerable<ISegmentM> GetSegments();
}

public static class MediaItemExtensions {
  public static IEnumerable<ISegmentM> GetSegments(this IEnumerable<IMediaItemM> items) =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetSegments());
}