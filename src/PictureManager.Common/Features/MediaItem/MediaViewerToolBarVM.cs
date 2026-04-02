using MH.Utils.BaseClasses;
using PictureManager.Common.Layout;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaViewerToolBarVM(MediaViewerVM mediaViewer, MainWindowVM mainWindow) : ObservableObject {
  public MediaViewerVM MediaViewer { get; } = mediaViewer;
  public MainWindowVM MainWindow { get; } = mainWindow;
}