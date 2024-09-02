using MH.Utils.BaseClasses;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaViewerToolBarVM(MediaViewerVM mediaViewer) : ObservableObject {
  public MediaViewerVM MediaViewer { get; } = mediaViewer;
}