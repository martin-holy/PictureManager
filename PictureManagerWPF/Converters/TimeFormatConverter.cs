using MH.UI.WPF.Converters;
using System.Windows.Data;
using PictureManager.Domain.Models;

namespace PictureManager.Converters;

public sealed class TimeFormatConverter : BaseMarkupExtensionConverter {
  private const string _position = "position";
  private const string _duration = "duration";

  public override object Convert(object value, object parameter) =>
    value is not int ms || parameter is null
      ? Binding.DoNothing
      : parameter switch {
        _position => VideoClipM.FormatPosition(ms),
        _duration => VideoClipM.FormatDuration(ms),
        _ => Binding.DoNothing
      } ;
}