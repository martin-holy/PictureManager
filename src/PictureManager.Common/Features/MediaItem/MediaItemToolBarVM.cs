using MH.Utils.BaseClasses;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Layout;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemToolBarVM(MediaItemsViewsVM mediaItemViews, MainWindowVM mainWindow, ImageComparerVM imageComparer) : ObservableObject {
  public MediaItemsViewsVM MediaItemViews { get; } = mediaItemViews;
  public MainWindowVM MainWindow { get; } = mainWindow;
  public ImageComparerVM ImageComparer { get; } = imageComparer;
}