using PictureManager.Dialogs;
using PictureManager.Domain.CatTreeViewModels;
using System;
using System.Windows.Input;

namespace PictureManager.Commands {
  public static class CatTreeViewCommands {
    public static RoutedUICommand ItemNewCommand { get; } = new() { Text = "New" };
    public static RoutedUICommand ItemRenameCommand { get; } = new() { Text = "Rename" };
    public static RoutedUICommand ItemDeleteCommand { get; } = new() { Text = "Delete" };
    public static RoutedUICommand GroupNewCommand { get; } = new() { Text = "New Group" };
    public static RoutedUICommand GroupRenameCommand { get; } = new() { Text = "Rename Group" };
    public static RoutedUICommand GroupDeleteCommand { get; } = new() { Text = "Delete Group" };
    public static RoutedUICommand SortCommand { get; } = new() { Text = "Sort" };

    public static void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, ItemNewCommand, ItemNew);
      CommandsController.AddCommandBinding(cbc, ItemRenameCommand, ItemRename);
      CommandsController.AddCommandBinding(cbc, ItemDeleteCommand, ItemDelete);
      CommandsController.AddCommandBinding(cbc, GroupNewCommand, GroupNew);
      CommandsController.AddCommandBinding(cbc, GroupRenameCommand, GroupRename);
      CommandsController.AddCommandBinding(cbc, GroupDeleteCommand, GroupDelete);
      CommandsController.AddCommandBinding(cbc, SortCommand, Sort);
    }

    public static void ItemNew(object parameter) {
      if (parameter is not ICatTreeViewItem item || CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat) return;

      var result = InputDialog.Open(
        cat.CategoryGroupIconName,
        "New Item",
        "Enter the name of the new Item.",
        string.Empty,
        answer => cat.ValidateNewItemTitle(item, answer),
        out var output);

      if (!result) return;

      try {
        var tvi = cat.ItemCreate(item, output);
        App.WMain.TreeViewCategories.TvCategories.ScrollTo(tvi);
      }
      catch (Exception ex) {
        ErrorDialog.Show(ex);
      }
    }

    public static void ItemRename(object parameter) {
      if (parameter is not ICatTreeViewItem item || CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat) return;

      var result = InputDialog.Open(
        cat.CategoryGroupIconName,
        "Rename Item",
        "Enter the new name for the Item.",
        cat.GetTitle(item),
        answer => cat.ValidateNewItemTitle((ICatTreeViewItem)item.Parent, answer),
        out var output);

      if (!result) return;

      try {
        cat.ItemRename(item, output);
        CatTreeViewUtils.OnAfterItemRename?.Invoke(parameter, EventArgs.Empty);
      }
      catch (Exception ex) {
        ErrorDialog.Show(ex);
      }
    }

    public static void ItemDelete(object parameter) {
      if (parameter is not ICatTreeViewItem item || CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat) return;

      if (!MessageDialog.Show(
        "Delete Confirmation",
        $"Do you really want to delete '{cat.GetTitle(item)}'?",
        true)) return;

      try {
        cat.ItemDelete(item);
        CatTreeViewUtils.OnAfterItemDelete?.Invoke(parameter, EventArgs.Empty);
      }
      catch (Exception ex) {
        ErrorDialog.Show(ex);
      }
    }

    public static void GroupNew(object parameter) {
      if (parameter is not ICatTreeViewItem item || CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat) return;

      var result = InputDialog.Open(
        cat.CategoryGroupIconName,
        "New Group",
        "Enter the name of the new Group.",
        string.Empty,
        answer => cat.ValidateNewGroupTitle(item, answer),
        out var output);

      if (!result) return;
      cat.GroupCreate(cat, output);
    }

    public static void GroupRename(object parameter) {
      if (parameter is not ICatTreeViewGroup item || CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat) return;

      var result = InputDialog.Open(
        cat.CategoryGroupIconName,
        "Rename Group",
        "Enter the new name for the Group.",
        cat.GetGroupTitle(item),
        answer => cat.ValidateNewGroupTitle((ICatTreeViewItem)item.Parent, answer),
        out var output);

      if (!result) return;
      cat.GroupRename(item, output);
    }

    public static void GroupDelete(object parameter) {
      if (parameter is not ICatTreeViewGroup item || CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat) return;

      if (!MessageDialog.Show(
        "Delete Confirmation",
        $"Do you really want to delete '{cat.GetGroupTitle(item)}'?",
        true)) return;

      cat.GroupDelete(item);
    }

    public static void Sort(object parameter) => CatTreeViewUtils.Sort(parameter as ICatTreeViewItem);
  }
}
