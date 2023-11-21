using System.Windows.Data;
using MH.UI.WPF.Converters;

namespace PictureManager.Converters; 

public class SegmentsViewWidthConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) =>
    value is int size && int.TryParse(parameter as string, out var count) ?
      (size + 1) * count + 15 + 2 + 1 //(size + 1) * count + ScrollBar + Margin + ToBeSure
      : Binding.DoNothing;
}