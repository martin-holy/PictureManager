using System.Windows;

namespace MH.UI.WPF.Converters;

public class GridLengthConverter : BaseConverter {
  private static readonly object _lock = new();
  private static GridLengthConverter _inst;
  public static GridLengthConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) =>
    new GridLength((double)value);

  public override object ConvertBack(object value, object parameter) =>
    ((GridLength)value).Value;
}