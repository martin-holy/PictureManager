using System.Linq;
using System.Windows.Input;
using PictureManager.Database;
using PictureManager.Dialogs;
using PictureManager.Patterns;
using PictureManager.ViewModel;

namespace PictureManager.Commands {
  public class TreeViewCommands: SingletonBase<TreeViewCommands> {
    public static RoutedUICommand CategoryGroupNewCommand { get; } = new RoutedUICommand { Text = "New Group" };
    public static RoutedUICommand CategoryGroupRenameCommand { get; } = new RoutedUICommand { Text = "Rename Group" };
    public static RoutedUICommand CategoryGroupDeleteCommand { get; } = new RoutedUICommand { Text = "Delete Group" };
    public static RoutedUICommand TagItemNewCommand { get; } = new RoutedUICommand { Text = "New" };
    public static RoutedUICommand TagItemRenameCommand { get; } = new RoutedUICommand { Text = "Rename" };
    public static RoutedUICommand TagItemDeleteCommand { get; } = new RoutedUICommand { Text = "Delete" };
    public static RoutedUICommand TagItemDeleteNotUsedCommand { get; } = new RoutedUICommand { Text = "Delete not used" };
    public static RoutedUICommand FolderNewCommand { get; } = new RoutedUICommand { Text = "New folder" };
    public static RoutedUICommand FolderRenameCommand { get; } = new RoutedUICommand { Text = "Rename" };
    public static RoutedUICommand FolderDeleteCommand { get; } = new RoutedUICommand { Text = "Delete" };
    public static RoutedUICommand FolderAddToFavoritesCommand { get; } = new RoutedUICommand { Text = "Add to Favorites" };
    public static RoutedUICommand FolderRemoveFromFavoritesCommand { get; } = new RoutedUICommand { Text = "Remove from Favorites" };
    public static RoutedUICommand FolderSetAsFolderKeywordCommand { get; } = new RoutedUICommand { Text = "Set as Folder Keyword" };
    public static RoutedUICommand ViewerIncludeFolderCommand { get; } = new RoutedUICommand { Text = "Include for Viewer" };
    public static RoutedUICommand ViewerExcludeFolderCommand { get; } = new RoutedUICommand { Text = "Exclude for Viewer" };
    public static RoutedUICommand ViewerRemoveFolderCommand { get; } = new RoutedUICommand { Text = "Remove" };
    public static RoutedUICommand GeoNameNewCommand { get; } = new RoutedUICommand { Text = "New" };
    public static RoutedUICommand ActivateFilterAndCommand { get; } = new RoutedUICommand { Text = "Filter And" };
    public static RoutedUICommand ActivateFilterOrCommand { get; } = new RoutedUICommand { Text = "Filter Or" };
    public static RoutedUICommand ActivateFilterNotCommand { get; } = new RoutedUICommand { Text = "Filter Not" };

    public void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, CategoryGroupNewCommand, CategoryGroupNew);
      CommandsController.AddCommandBinding(cbc, CategoryGroupRenameCommand, CategoryGroupRename);
      CommandsController.AddCommandBinding(cbc, CategoryGroupDeleteCommand, CategoryGroupDelete);
      CommandsController.AddCommandBinding(cbc, TagItemNewCommand, TagItemNew);
      CommandsController.AddCommandBinding(cbc, TagItemRenameCommand, TagItemRename);
      CommandsController.AddCommandBinding(cbc, TagItemDeleteCommand, TagItemDelete);
      CommandsController.AddCommandBinding(cbc, TagItemDeleteNotUsedCommand, TagItemDeleteNotUsed);
      CommandsController.AddCommandBinding(cbc, FolderNewCommand, FolderNew);
      CommandsController.AddCommandBinding(cbc, FolderRenameCommand, FolderRename);
      CommandsController.AddCommandBinding(cbc, FolderDeleteCommand, FolderDelete);
      CommandsController.AddCommandBinding(cbc, FolderAddToFavoritesCommand, FolderAddToFavorites);
      CommandsController.AddCommandBinding(cbc, FolderRemoveFromFavoritesCommand, FolderRemoveFromFavorites);
      CommandsController.AddCommandBinding(cbc, FolderSetAsFolderKeywordCommand, FolderSetAsFolderKeyword);
      CommandsController.AddCommandBinding(cbc, ViewerIncludeFolderCommand, ViewerIncludeFolder);
      CommandsController.AddCommandBinding(cbc, ViewerExcludeFolderCommand, ViewerExcludeFolder);
      CommandsController.AddCommandBinding(cbc, ViewerRemoveFolderCommand, ViewerRemoveFolder);
      CommandsController.AddCommandBinding(cbc, GeoNameNewCommand, GeoNameNew);
      CommandsController.AddCommandBinding(cbc, ActivateFilterAndCommand, ActivateFilterAnd);
      CommandsController.AddCommandBinding(cbc, ActivateFilterOrCommand, ActivateFilterOr);
      CommandsController.AddCommandBinding(cbc, ActivateFilterNotCommand, ActivateFilterNot);
    }

    private static void CategoryGroupNew(object parameter) {
      (parameter as BaseCategoryItem)?.GroupNewOrRename(null, false);
    }

    private static void CategoryGroupRename(object parameter) {
      var group = parameter as CategoryGroup;
      (group?.Parent as BaseCategoryItem)?.GroupNewOrRename(group, true);
    }

    private static void CategoryGroupDelete(object parameter) {
      var group = parameter as CategoryGroup;
      (group?.Parent as BaseCategoryItem)?.GroupDelete(group);
    }

    private static void TagItemNew(object parameter) {
      var item = parameter as BaseTreeViewItem;
      (item?.GetTopParent() as BaseCategoryItem)?.ItemNewOrRename(item, false);
    }

    private static void TagItemRename(object parameter) {
      var item = parameter as BaseTreeViewItem;
      (item?.GetTopParent() as BaseCategoryItem)?.ItemNewOrRename(item, true);
    }

    private static void TagItemDelete(object parameter) {
      if (!(parameter is BaseTreeViewItem item)) return;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you really want to delete '{item.Title}'?", true)) return;
      (item.GetTopParent() as BaseCategoryItem)?.ItemDelete(item);
    }

    private static void TagItemDeleteNotUsed(object parameter) {
      if (!(parameter is BaseTreeViewItem item)) return;
      if (!(item.GetTopParent() is BaseCategoryItem topParent)) return;

      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete not used items in '{item.Title}'?", true)) return;

      switch (topParent.Category) {
        case Category.People: {
            foreach (var person in item.Items.Cast<Person>().Where(x => x.MediaItems.Count == 0).ToArray())
              topParent.ItemDelete(person);

            break;
          }
        case Category.Keywords: {
            foreach (var keyword in item.Items.Cast<Keyword>().Where(x => x.MediaItems.Count == 0).ToArray())
              topParent.ItemDelete(keyword);

            break;
          }
      }
    }

    private static void ViewerIncludeFolder(object parameter) {
      ((Viewer)parameter).AddFolder(true);
    }

    private static void ViewerExcludeFolder(object parameter) {
      ((Viewer)parameter).AddFolder(false);
    }

    private static void ViewerRemoveFolder(object parameter) {
      var folder = (BaseTreeViewItem)parameter;
      folder.Parent?.Items.Remove(folder);
      App.Core.Viewers.Helper.Table.SaveToFile();
    }

    private static void FolderNew(object parameter) {
      ((Folder)parameter).NewOrRename(false);
    }

    private static void FolderRename(object parameter) {
      ((Folder)parameter).NewOrRename(true);
    }

    private static void FolderDelete(object parameter) {
      var folder = (Folder)parameter;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you really want to delete '{folder.Title}' folder?", true)) return;

      App.Core.Folders.DeleteRecord(folder, true);
      // reload FolderKeywords
      App.Core.FolderKeywords.Load();
    }

    private static void FolderAddToFavorites(object parameter) {
      App.Core.FavoriteFolders.Add((Folder)parameter);
    }

    private static void FolderRemoveFromFavorites(object parameter) {
      App.Core.FavoriteFolders.Remove((FavoriteFolder)parameter);
    }

    private static void FolderSetAsFolderKeyword(object parameter) {
      ((Folder)parameter).IsFolderKeyword = true;
      App.Core.Folders.Helper.Table.SaveToFile();
      App.Core.FolderKeywords.Load();
    }

    private void GeoNameNew(object parameter) {
      if (!GeoNames.AreSettingsSet()) return;

      var inputDialog = new InputDialog {
        Owner = App.WMain,
        IconName = IconName.LocationCheckin,
        Title = "GeoName latitude and longitude",
        Question = "Enter in format: N36.75847,W3.84609",
        Answer = ""
      };

      inputDialog.BtnDialogOk.Click += delegate { inputDialog.DialogResult = true; };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        ((GeoNames)parameter).New(inputDialog.Answer);
      }
    }

    private static void ActivateFilterAnd(object parameter) {
      App.Core.ActivateFilter((BaseTreeViewItem)parameter, BackgroundBrush.AndThis);
    }

    private static void ActivateFilterOr(object parameter) {
      App.Core.ActivateFilter((BaseTreeViewItem)parameter, BackgroundBrush.OrThis);
    }

    private static void ActivateFilterNot(object parameter) {
      App.Core.ActivateFilter((BaseTreeViewItem)parameter, BackgroundBrush.Hidden);
    }

  }
}
