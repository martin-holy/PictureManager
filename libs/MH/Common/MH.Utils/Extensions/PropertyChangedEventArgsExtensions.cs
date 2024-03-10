using System;
using System.ComponentModel;

namespace MH.Utils.Extensions;

public static class PropertyChangedEventArgsExtensions {
  public static bool Is(this PropertyChangedEventArgs e, string name) =>
    name.Equals(e.PropertyName, StringComparison.Ordinal);
}