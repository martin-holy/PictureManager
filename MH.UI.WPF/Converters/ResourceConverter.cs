using System.Collections.Generic;
using System.Windows;

namespace MH.UI.WPF.Converters;

public class ResourceConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) {
    value = TryConvertValue(value, parameter);

    return value == null
      ? DependencyProperty.UnsetValue
      : TryFindResource(Application.Current.Resources, value)
        ?? DependencyProperty.UnsetValue;
  }

  public static object TryConvertValue(object value, object parameter) {
    if (value == null) return null;
    if (parameter is Dictionary<object, object> dict) {
      if (!dict.TryGetValue(value, out var dicValue))
        if (!dict.TryGetValue(value.GetType(), out dicValue))
          dict.TryGetValue("default", out dicValue);

      return dicValue;
    }

    return value;
  }

  public static object TryFindResource(ResourceDictionary dictionary, object value) {
    if (dictionary.Contains(value))
      return dictionary[value];

    object res;
    foreach (var item in dictionary.Values) {
      if (item is not FrameworkTemplate ft) continue;
      res = TryFindResource(ft.Resources, value);
      if (res != null)
        return res;
    }

    foreach (var mergedDictionary in dictionary.MergedDictionaries) {
      res = TryFindResource(mergedDictionary, value);
      if (res != null)
        return res;
    }

    return null;
  }
}