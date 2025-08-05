using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Folder;
using System;
using System.Collections.Generic;

namespace PictureManager.Android.Utils;

public static class MenuFactory {
  private static readonly Dictionary<Type, PopupWindow> _menus = [];

  public static PopupWindow? GetMenu(View parent, object? item) {
    if (item == null) return null;
    var type = item.GetType();
    if (_menus.TryGetValue(type, out var menu)) return menu; // TODO bind item before return

    var root = item switch {
      DriveM => _createDriveMenu(),
      FolderM => _createFolderMenu(),
      _ => null
    };

    if (root == null) return null;
    menu = ButtonMenu.CreateMenu(parent.Context!, parent, root);
    _menus.Add(type, menu);

    return menu; // TODO bind item before return
  }

  private static MenuItem _createDriveMenu() {
    var root = new MenuItem(null, string.Empty);
    root.Add(new(TreeCategory.ItemCreateCommand));
    return root;
  }

  private static MenuItem _createFolderMenu() {
    var root = new MenuItem(null, string.Empty);
    root.Add(new(TreeCategory.ItemCreateCommand));
    root.Add(new(TreeCategory.ItemRenameCommand));
    root.Add(new(TreeCategory.ItemDeleteCommand));
    return root;
  }
}