using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models.MediaItems;

namespace PictureManager.Domain.ViewModels;

public sealed class MediaItemsVM {
  public static IImageSourceConverter<MediaItemM> ThumbConverter { get; set; }
}