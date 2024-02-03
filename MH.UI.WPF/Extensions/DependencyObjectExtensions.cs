using System.ComponentModel;
using System.Windows;

namespace MH.UI.WPF.Extensions;

public static class DependencyObjectExtensions {
  public static bool HasAttachedProperty(this DependencyObject obj, DependencyProperty property) =>
    (TypeDescriptor.GetProperties(obj).Find(property.Name, false) as DependencyPropertyDescriptor)
    ?.DependencyProperty == property;
}