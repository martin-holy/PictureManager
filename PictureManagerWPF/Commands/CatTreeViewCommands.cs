using System.Windows.Input;
using PictureManager.Dialogs;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Patterns;

namespace PictureManager.Commands {
  public class CatTreeViewCommands: Singleton<CatTreeViewCommands> {
    public static RoutedUICommand GroupNewCommand { get; } = new RoutedUICommand { Text = "New Group" };
    public static RoutedUICommand GroupRenameCommand { get; } = new RoutedUICommand { Text = "Rename Group" };
    public static RoutedUICommand GroupDeleteCommand { get; } = new RoutedUICommand { Text = "Delete Group" };
    public static RoutedUICommand ItemNewCommand { get; } = new RoutedUICommand { Text = "New" };
    public static RoutedUICommand ItemRenameCommand { get; } = new RoutedUICommand { Text = "Rename" };
    public static RoutedUICommand ItemDeleteCommand { get; } = new RoutedUICommand { Text = "Delete" };

    public void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, GroupNewCommand, GroupNew);
      CommandsController.AddCommandBinding(cbc, GroupRenameCommand, GroupRename);
      CommandsController.AddCommandBinding(cbc, GroupDeleteCommand, GroupDelete);
      CommandsController.AddCommandBinding(cbc, ItemNewCommand, ItemNew);
      CommandsController.AddCommandBinding(cbc, ItemRenameCommand, ItemRename);
      CommandsController.AddCommandBinding(cbc, ItemDeleteCommand, ItemDelete);
    }

    public static void GroupNew(object parameter) {
      if (!(parameter is ICatTreeViewBaseItem item) ||
          !(CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory cat)) return;

      var result = InputDialog.Open(
        cat.CategoryGroupIconName,
        "New Group",
        "Enter the name of the new Group.",
        string.Empty,
        answer => cat.ValidateNewGroupTitle(item, answer),
        out var output);

      if (!result) return;
      cat.GroupCreate(item, output);
    }

    public static void GroupRename(object parameter) {
      if (!(parameter is ICatTreeViewGroup item) ||
          !(CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory cat)) return;

      var result = InputDialog.Open(
        cat.CategoryGroupIconName,
        "Rename Group",
        "Enter the new name for the Group.",
        item.Title,
        answer => cat.ValidateNewGroupTitle(item.Parent, answer),
        out var output);

      if (!result) return;
      cat.GroupRename(item, output);
    }

    public static void GroupDelete(object parameter) {
      if (!(parameter is ICatTreeViewGroup item) ||
          !(CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory cat)) return;

      if (!MessageDialog.Show(
        "Delete Confirmation",
        $"Do you really want to delete '{item.Title}'?",
        true)) return;

      cat.GroupDelete(item);
    }

    public static void ItemNew(object parameter) {
      if (!(parameter is ICatTreeViewBaseItem item) ||
          !(CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory cat)) return;

      var result = InputDialog.Open(
        cat.CategoryGroupIconName,
        "New Item",
        "Enter the name of the new Item.",
        string.Empty,
        answer => cat.ValidateNewItemTitle(item, answer),
        out var output);

      if (!result) return;
      cat.ItemCreate(item, output);
    }

    public static void ItemRename(object parameter) {
      if (!(parameter is ICatTreeViewBaseItem item) ||
          !(CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory cat)) return;

      var result = InputDialog.Open(
        cat.CategoryGroupIconName,
        "Rename Item",
        "Enter the new name for the Item.",
        item.Title,
        answer => cat.ValidateNewItemTitle(item.Parent, answer),
        out var output);

      if (!result) return;
      cat.ItemRename(item, output);
    }

    public static void ItemDelete(object parameter) {
      if (!(parameter is ICatTreeViewBaseItem item) ||
          !(CatTreeViewUtils.GetTopParent(item) is ICatTreeViewCategory cat)) return;
      
      if (!MessageDialog.Show(
        "Delete Confirmation", 
        $"Do you really want to delete '{item.Title}'?", 
        true)) return;
      
      cat.ItemDelete(item);
    }
  }
}
