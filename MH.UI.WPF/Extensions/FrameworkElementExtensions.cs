using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MH.UI.WPF.Extensions;

public static class FrameworkElementExtensions {
  public static T FindTemplatedParent<T>(this FrameworkElement child) where T : FrameworkElement {
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

  public static FrameworkElement FindTopTemplatedParent(this FrameworkElement child) {
    var parent = child?.TemplatedParent as FrameworkElement;
    while (true) {
      if (parent == null) return null;
      if (parent.TemplatedParent == null) return parent;
      parent = parent.TemplatedParent as FrameworkElement;
    }
  }

  public static CroppedBitmap ToBitmap(this FrameworkElement fe) {
    var offset = fe.TranslatePoint(new(0, 0), (UIElement)fe.Parent);
    var ox = (int)Math.Round(offset.X, 0);
    var oy = (int)Math.Round(offset.Y, 0);
    var aw = (int)Math.Round(fe.ActualWidth, 0);
    var ah = (int)Math.Round(fe.ActualHeight, 0);

    var bmp = new RenderTargetBitmap(aw + ox, ah + oy, 96, 96, PixelFormats.Pbgra32);
    bmp.Render(fe);

    return new(bmp, new(ox, oy, aw, ah));
  }
}