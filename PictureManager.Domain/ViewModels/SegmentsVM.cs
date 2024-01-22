using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.ViewModels;

public sealed class SegmentsVM {
  public static IImageSourceConverter<SegmentM> ThumbConverter { get; set; }
}