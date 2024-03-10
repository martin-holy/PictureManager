using System;

namespace MH.Utils.Extensions;

public static class DoubleExtensions {
  public static double RoundTo(this double value, double maxDigits) {
    var precision = 0;
    while (maxDigits * Math.Pow(10, precision) != Math.Round(maxDigits * Math.Pow(10, precision)))
      precision++;

    return Math.Round(Math.Round(value / maxDigits, 0) * maxDigits, precision);
  }
}