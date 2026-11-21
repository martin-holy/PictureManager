using MH.UI.WPF.Utils;
using Microsoft.Xaml.Behaviors;
using PictureManager.Common.Utils;
using System.Windows;

namespace PictureManager.Windows.WPF.Utils;

public sealed class TreeContextMenuAction : TriggerAction<FrameworkElement> {
  protected override void Invoke(object parameter) {
    var element = AssociatedObject;
    var item = element.DataContext;

    element.ContextMenu = item is null
      ? null
      : TreeContextMenuBuilder.Create(MenuFactory.GetMenu(item));
  }
}