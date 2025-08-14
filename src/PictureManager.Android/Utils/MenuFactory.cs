using Android.Views;
using Android.Widget;
using MH.UI.BaseClasses;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.FavoriteFolder;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.FolderKeyword;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Viewer;
using System;
using System.Collections.Generic;

namespace PictureManager.Android.Utils;

public static class MenuFactory {
  private static readonly Dictionary<Type, (MenuItem menuRootVM, PopupWindow menuV)> _menus = [];

  public static PopupWindow? GetMenu(View parent, object? item) {
    if (item == null) return null;
    var type = item.GetType();
    if (_menus.TryGetValue(type, out var menu))
      return _bindMenu(menu.menuRootVM, menu.menuV, item);

    var menuRootVM = item switch {
      DriveM => _createDriveMenu(),
      FolderM => _createFolderMenu(),
      FavoriteFolderM => null,
      PersonTreeCategory => _createPersonTreeCategoryMenu(),
      PersonM => null,
      FolderKeywordTreeCategory => null,
      KeywordTreeCategory => _createKeywordTreeCategoryMenu(),
      KeywordM => null,
      GeoNameTreeCategory => null,
      GeoNameM => null,
      ViewerTreeCategory => null,
      ViewerM => null,
      KeywordCategoryGroupM => null,
      PersonCategoryGroupM => null,
      _ => null
    };

    if (menuRootVM == null) return null;
    var menuV = MH.UI.Android.Utils.MenuFactory.CreateMenu(parent.Context!, parent, menuRootVM);
    _menus.Add(type, new(menuRootVM, menuV));

    return _bindMenu(menuRootVM, menuV, item);
  }

  private static PopupWindow? _bindMenu(MenuItem menuRootVM, PopupWindow menuV, object? item) {
    foreach (var menuItem in menuRootVM.Flatten())
      menuItem.CommandParameter = item;

    return menuV;
  }

  // Drive
  private static MenuItem _createDriveMenu() {
    var root = new MenuItem(null, string.Empty);
    root.Add(new(TreeCategory.ItemCreateCommand));
    return root;
  }

  // Folder
  private static MenuItem _createFolderMenu() {
    var root = new MenuItem(null, string.Empty);
    root.Add(new(TreeCategory.ItemCreateCommand));
    root.Add(new(TreeCategory.ItemRenameCommand));
    root.Add(new(TreeCategory.ItemDeleteCommand));
    return root;
  }

  // Person TreeCategory
  private static MenuItem _createPersonTreeCategoryMenu() {
    var root = new MenuItem(null, string.Empty);
    root.Add(new(TreeCategory.ItemCreateCommand));
    root.Add(new(TreeCategory.GroupCreateCommand));
    return root;
  }

  // Keyword TreeCategory
  private static MenuItem _createKeywordTreeCategoryMenu() {
    var root = new MenuItem(null, string.Empty);
    root.Add(new(TreeCategory.ItemCreateCommand));
    root.Add(new(TreeCategory.GroupCreateCommand));
    return root;
  }
}