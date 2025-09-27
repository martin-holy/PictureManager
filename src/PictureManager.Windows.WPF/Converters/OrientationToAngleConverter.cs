using MH.UI.WPF.Converters;
using MH.Utils;
using MH.Utils.Extensions;

namespace PictureManager.Windows.WPF.Converters;

public class OrientationToAngleConverter : BaseConverter {
  private static readonly object _lock = new();
  private static OrientationToAngleConverter? _inst;
  public static OrientationToAngleConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object? value, object? parameter) =>
    (value is Imaging.Orientation o ? o : Imaging.Orientation.Normal).ToAngle();
}