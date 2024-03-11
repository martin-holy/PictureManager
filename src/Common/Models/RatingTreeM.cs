using MH.Utils.BaseClasses;

namespace PictureManager.Common.Models;

public sealed class RatingTreeM(int value) : TreeItem(Res.IconStar, value.ToString()) {
  public RatingM Rating { get; } = new(value);
}