namespace PictureManager.Domain.Models;

public sealed class RatingM(int value) {
  public int Value { get; } = value;
}