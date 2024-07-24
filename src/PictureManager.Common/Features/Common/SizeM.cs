using MH.Utils.BaseClasses;

namespace PictureManager.Common.Features.Common;

public class SizeM(string icon, string text, int min, int max) : ListItem(icon, text) {
  public int Min { get; set; } = min;
  public int Max { get; set; } = max;

  public bool Fits(int size) => size >= Min && size <= Max;
}