using System.Collections.ObjectModel;

namespace PictureManager.Domain.Models {
  public class MediaItemsRow {
    public ObservableCollection<MediaItem> Items { get; } = new ObservableCollection<MediaItem>();
  }
}
