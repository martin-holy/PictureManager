using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models;

public sealed class RatingTreeM : TreeItem {
  public RatingM Rating { get; }

  public RatingTreeM(int value) : base(Res.IconStar, value.ToString()) {
    Rating = new(value);
  }
}