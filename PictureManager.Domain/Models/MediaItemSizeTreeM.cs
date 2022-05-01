using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class MediaItemSizeTreeM : TreeItem {
    private ThumbnailsGridM _currentGrid;

    public ThumbnailsGridM CurrentGrid { get => _currentGrid; set { _currentGrid = value; OnPropertyChanged(); } }
    public RelayCommand<object> RangeChangedCommand { get; }

    public MediaItemSizeTreeM() {
      RangeChangedCommand = new(
        () => {
            CurrentGrid.FilterSize.AllSizes = false;
            _ = CurrentGrid.ReapplyFilter();
          },
        () => CurrentGrid != null
      );
    }
  }
}
