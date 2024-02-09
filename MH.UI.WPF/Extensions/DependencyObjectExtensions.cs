using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace MH.UI.WPF.Extensions;

public static class DependencyObjectExtensions {
  public static T GetChildOfType<T>(this DependencyObject o) where T : DependencyObject {
    if (o == null) return null;

    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++) {
      var child = VisualTreeHelper.GetChild(o, i);
      var result = child as T ?? GetChildOfType<T>(child);
      if (result != null) return result;
    }

    return null;
  }

  public static bool HasAttachedProperty(this DependencyObject obj, DependencyProperty property) =>
    (TypeDescriptor.GetProperties(obj).Find(property.Name, false) as DependencyPropertyDescriptor)
    ?.DependencyProperty == property;

  public static T TryFindParent<T>(this DependencyObject child) where T : DependencyObject {
    while (true) {
      var parent = VisualTreeHelper.GetParent(child);
      if (parent == null) return null;
      if (parent is T val) return val;
      child = parent;
    }
  }
}