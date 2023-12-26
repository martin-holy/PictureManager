using MH.UI.Controls;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public sealed class TimeFormatConverter : BaseMarkupExtensionConverter {
  private const string _position = "position";
  private const string _duration = "duration";

  public override object Convert(object value, object parameter) =>
    value is not int ms || parameter is null
      ? Binding.DoNothing
      : parameter switch {
        _position => MediaPlayer.FormatPosition(ms),
        _duration => MediaPlayer.FormatDuration(ms),
        _ => Binding.DoNothing
      } ;
}