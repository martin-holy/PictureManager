using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class MediaItemDeletedEventArgs {
    public MediaItemM MediaItem { get; }

    public MediaItemDeletedEventArgs(MediaItemM mediaItem) {
      MediaItem = mediaItem;
    }
  }
}
