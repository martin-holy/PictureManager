using MH.Utils;
using MH.Utils.Interfaces;
using System.Windows;

namespace MH.UI.WPF.Converters;

public class TreeMarginConverter : BaseConverter {
  private static readonly object _lock = new();
  private static TreeMarginConverter _inst;
  public static TreeMarginConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) {
    var level = value is ITreeItem ti ? ti.GetLevel() : 0;
    var length = int.TryParse(parameter as string, out var l) ? l : 0;

    return new Thickness(length * level, 0.0, 0.0, 0.0);
  }
}