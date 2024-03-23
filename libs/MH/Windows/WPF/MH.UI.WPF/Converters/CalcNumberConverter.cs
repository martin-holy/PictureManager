using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class CalcNumberConverter : BaseConverter {
  private static readonly object _lock = new();
  private static CalcNumberConverter _add;
  private static CalcNumberConverter _subtract;
  private static CalcNumberConverter _multiply;
  private static CalcNumberConverter _divide;

  public static CalcNumberConverter Add { get {
    lock (_lock) {
      return _add ??= new() { DoAdd = true };
    } } }

  public static CalcNumberConverter Subtract { get {
    lock (_lock) {
      return _subtract ??= new() { DoSubtract = true };
    } } }

  public static CalcNumberConverter Multiply { get {
    lock (_lock) {
      return _multiply ??= new() { DoMultiply = true };
    } } }

  public static CalcNumberConverter Divide { get {
    lock (_lock) {
      return _divide ??= new() { DoDivide = true };
    } } }

  public bool DoAdd { get; init; }
  public bool DoSubtract { get; init; }
  public bool DoMultiply { get; init; }
  public bool DoDivide { get; init; }

  public override object Convert(object value, object parameter) {
    if (value is not double v || parameter is not double p)
      return Binding.DoNothing;

    if (DoAdd) return v + p;
    if (DoSubtract) return v - p;
    if (DoMultiply) return v * p;
    if (DoDivide) return v / p;

    return Binding.DoNothing;
  }
}