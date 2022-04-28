using System;
using System.Windows;
using System.Windows.Media;

namespace MH.UI.WPF.Utils {
  public static class Extensions {
    public static T GetChildOfType<T>(this DependencyObject o) where T : DependencyObject {
      if (o == null) return null;

      for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++) {
        var child = VisualTreeHelper.GetChild(o, i);
        var result = (child as T) ?? GetChildOfType<T>(child);
        if (result != null) return result;
      }

      return null;
    }

    public static T FindTemplatedParent<T>(FrameworkElement child) where T : FrameworkElement {
      while (true) {
        if (child?.TemplatedParent == null) return null;
        if (child.TemplatedParent is T parent) return parent;
        child = (FrameworkElement)child.TemplatedParent;
      }
    }

    public static T FindThisOrParent<T>(FrameworkElement child, string name) where T : FrameworkElement {
      while (true) {
        if (child == null) return null;
        if (child is T element && element.Name.Equals(name, StringComparison.Ordinal)) return element;
        child = (FrameworkElement)(child.Parent ?? child.TemplatedParent);
      }
    }
  }
}
