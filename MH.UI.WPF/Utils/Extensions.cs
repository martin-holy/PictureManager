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

    public static T TryFindParent<T>(this DependencyObject child) where T : DependencyObject {
      DependencyObject parentObject = VisualTreeHelper.GetParent(child);
      if (parentObject == null)
        return null;

      T val = parentObject as T;
      return val ?? parentObject.TryFindParent<T>();
    }

    public static T FindChild<T>(this DependencyObject parent, string childName = null) where T : DependencyObject {
      if (parent == null) {
        return null;
      }

      T val = null;
      int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      for (int i = 0; i < childrenCount; i++) {
        DependencyObject child = VisualTreeHelper.GetChild(parent, i);
        T val2 = child as T;
        if (val2 == null) {
          val = child.FindChild<T>(childName);
          if (val != null) {
            break;
          }

          continue;
        }

        if (!string.IsNullOrEmpty(childName)) {
          IFrameworkInputElement frameworkInputElement = child as IFrameworkInputElement;
          if (frameworkInputElement != null && frameworkInputElement.Name == childName) {
            val = (T)child;
            break;
          }

          val = child.FindChild<T>(childName);
          if (val != null) {
            break;
          }

          continue;
        }

        val = (T)child;
        break;
      }

      return val;
    }
  }
}
