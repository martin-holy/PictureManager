using System.Globalization;
using System.Windows.Data;

namespace MH.UI.WPF.Utils;

public class CultureAwareBinding : Binding {
  public CultureAwareBinding() {
    ConverterCulture = CultureInfo.CurrentCulture;
  }
}