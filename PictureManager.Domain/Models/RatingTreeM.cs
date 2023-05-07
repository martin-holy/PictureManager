using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class RatingTreeM : TreeItem {
    public int Value { get; }

    public RatingTreeM(int value) {
      Value = value;
    }
  }
}
