using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class ThumbnailsGridsM : ObservableObject {
    private ThumbnailsGridM _current;

    public ObservableCollection<ThumbnailsGridM> All { get; } = new();
    public ThumbnailsGridM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
    public double DefaultThumbScale { get; set; } = 1.0;

    public ThumbnailsGridM AddThumbnailsGrid(MediaItemsM mediaItemsM, TitleProgressBarM progressBar) {
      var grid = new ThumbnailsGridM(mediaItemsM, progressBar, DefaultThumbScale);
      All.Add(grid);
      Current = ThumbnailsGridM.ActivateThumbnailsGrid(Current, grid);

      return grid;
    }

    public void RemoveMediaItem(MediaItemM item) {
      foreach (var grid in All)
        grid.Remove(item, Current == grid);
    }

    public async Task SetCurrentGrid(ThumbnailsGridM grid) {
      Current = ThumbnailsGridM.ActivateThumbnailsGrid(Current, grid);

      if (Current == null) return;

      Current.UpdateSelected();
      
      if (Current.NeedReload == true) {
        await Current.ThumbsGridReloadItems();
        Current.UpdatePositionSlashCount();
      }
    }
  }
}
