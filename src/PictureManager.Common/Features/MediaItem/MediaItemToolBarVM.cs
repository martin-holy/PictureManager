using MH.Utils.BaseClasses;
using PictureManager.Common.Layout;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemToolBarVM(MediaItemsViewsVM mediaItemViews, MainWindowVM mainWindow) : ObservableObject {
  public MediaItemsViewsVM MediaItemViews { get; } = mediaItemViews;
  public MainWindowVM MainWindow { get; } = mainWindow;
}