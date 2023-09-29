namespace MH.UI.WPF.Converters;

public class EqualityConverter : BaseMarkupExtensionMultiConverter {
  public override object Convert(object[] values, object parameter) {
    if (values is [double a, string op, double b])
      switch (op.ToLower()) {
        case "eq": return a == b;
        case "ne": return a != b;
        case "gt": return a > b;
        case "ge": return a >= b;
        case "lt": return a < b;
        case "le": return a <= b;
      }

    return false;
  }
}