using MH.UI.WPF.Converters;
using PictureManager.Domain.Models.MediaItems;

namespace PictureManager.Converters;

public class OrientationToAngleConverter : BaseConverter {
  private static readonly object _lock = new();
  private static OrientationToAngleConverter _inst;
  public static OrientationToAngleConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) =>
    MediaItemsM.OrientationToAngle(value is int i ? i : 0);
}