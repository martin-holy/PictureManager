using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class DoubleEqualityConverter : BaseConverter {
  private static readonly object _lock = new();
  private static DoubleEqualityConverter? _isEqual;
  private static DoubleEqualityConverter? _isNotEqual;
  private static DoubleEqualityConverter? _isGreaterThan;
  private static DoubleEqualityConverter? _isGreaterOrEqual;
  private static DoubleEqualityConverter? _isLessThan;
  private static DoubleEqualityConverter? _isLessOrEqual;

  public static DoubleEqualityConverter IsEqual { get {
    lock (_lock) {
      return _isEqual ??= new() { DoIsEqual = true };
    } } }

  public static DoubleEqualityConverter IsNotEqual { get {
    lock (_lock) {
      return _isNotEqual ??= new() { DoIsNotEqual = true };
    } } }

  public static DoubleEqualityConverter IsGreaterThan { get {
    lock (_lock) {
      return _isGreaterThan ??= new() { DoIsGreaterThan = true };
    } } }

  public static DoubleEqualityConverter IsGreaterOrEqual { get {
    lock (_lock) {
      return _isGreaterOrEqual ??= new() { DoIsGreaterOrEqual = true };
    } } }

  public static DoubleEqualityConverter IsLessThan { get {
    lock (_lock) {
      return _isLessThan ??= new() { DoIsLessThan = true };
    } } }

  public static DoubleEqualityConverter IsLessOrEqual { get {
    lock (_lock) {
      return _isLessOrEqual ??= new() { DoIsLessOrEqual = true };
    } } }

  public bool DoIsEqual { get; init; }
  public bool DoIsNotEqual { get; init; }
  public bool DoIsGreaterThan { get; init; }
  public bool DoIsGreaterOrEqual { get; init; }
  public bool DoIsLessThan { get; init; }
  public bool DoIsLessOrEqual { get; init; }

  public override object? Convert(object? value, object? parameter) {
    if (value is not double v || parameter is not double p)
      return Binding.DoNothing;

    if (DoIsEqual) return v == p;
    if (DoIsNotEqual) return v != p;
    if (DoIsGreaterThan) return v > p;
    if (DoIsGreaterOrEqual) return v >= p;
    if (DoIsLessThan) return v < p;
    if (DoIsLessOrEqual) return v <= p;

    return Binding.DoNothing;
  }
}