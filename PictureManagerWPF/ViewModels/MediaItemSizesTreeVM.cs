using MH.UI.WPF.BaseClasses;

namespace PictureManager.ViewModels {
  public static class MediaItemSizesTreeVM {
    public static readonly RelayCommand<object> RangeChangedCommand = new(
      () => {
          App.Core.ThumbnailsGridsM.Current.FilterSize.AllSizes = false;
          _ = App.Core.ThumbnailsGridsM.Current.ReapplyFilter();
        },
      () => App.Core.ThumbnailsGridsM.Current != null
    );
  }
}
