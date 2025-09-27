using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System.Threading.Tasks;
using Orientation = MH.Utils.Imaging.Orientation;

namespace PictureManager.Common.Features.MediaItem;

public sealed class RotationDialog : Dialog {
  private static RotationDialog? _inst;

  public RelayCommand Rotate90Command { get; }
  public RelayCommand Rotate180Command { get; }
  public RelayCommand Rotate270Command { get; }

  public RotationDialog() : base("Rotation", Res.IconImageMultiple) {
    Rotate90Command = new(() => Result = (int)Orientation.Rotate90, Res.IconRotateLeft);
    Rotate180Command = new(() => Result = (int)Orientation.Rotate180, Res.IconRotateClockwise);
    Rotate270Command = new(() => Result = (int)Orientation.Rotate270, Res.IconRotateRight);
  }

  public static async Task<Orientation> Open() {
    _inst ??= new();
    var result = await ShowAsync(_inst);

    return result == 0
      ? Orientation.Normal
      : (Orientation)result;
  }
}