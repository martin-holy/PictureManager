using System.Windows.Data;
using MH.UI.WPF.Converters;

namespace PictureManager.Converters; 

public class SegmentsViewWidthConverter : BaseConverter {
  private static readonly object _lock = new();
  private static SegmentsViewWidthConverter _inst;
  public static SegmentsViewWidthConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) =>
    value is int size && int.TryParse(parameter as string, out var count) ?
      (size + 1) * count + 15 + 2 + 1 //(size + 1) * count + ScrollBar + Margin + ToBeSure
      : Binding.DoNothing;
}