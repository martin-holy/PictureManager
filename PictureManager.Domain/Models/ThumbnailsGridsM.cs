using System.Collections.ObjectModel;
using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class ThumbnailsGridsM : ObservableObject {
    private ThumbnailsGridM _current;

    public ObservableCollection<ThumbnailsGridM> All { get; } = new();
    public ThumbnailsGridM Current { get => _current; set { _current = value; OnPropertyChanged(); } }

    public ThumbnailsGridM AddThumbnailsGrid() {
      var grid = new ThumbnailsGridM();
      All.Add(grid);
      Current = ThumbnailsGridM.ActivateThumbnailsGrid(Current, grid);

      return grid;
    }

    public void RemoveMediaItem(MediaItemM item) {
      foreach (var grid in All)
        grid.Remove(item, Current == grid);
    }
  }
}
