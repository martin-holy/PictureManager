using MH.Utils.BaseClasses;

namespace PictureManager.Common.Features.MediaItem;

public sealed class SlideshowToolBarVM(SlideshowVM slideshow) : ObservableObject {
  public SlideshowVM Slideshow { get; } = slideshow;
}