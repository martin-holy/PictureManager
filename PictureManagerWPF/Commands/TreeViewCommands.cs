using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Patterns;
using PictureManager.Properties;
using PictureManager.ViewModels;

namespace PictureManager.Commands {
  public class TreeViewCommands: Singleton<TreeViewCommands> {
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
      if (!(parameter is BaseCategoryItem parent)) return;

      var result = InputDialog.Open(
        parent.CategoryGroupIconName,
        "New Group",
        "Enter the name of the new group.",
        string.Empty,
        answer => parent.ValidateNewGroupTitle(answer),
        out var output);

      if (!result) return;
      parent.GroupCreate(output);
    }

    private static void CategoryGroupRename(object parameter) {
      if (!(parameter is CategoryGroup group) || !(group.Parent is BaseCategoryItem parent)) return;

      var result = InputDialog.Open(
        parent.CategoryGroupIconName,
        "Rename Group",
        "Enter the new name for the group.",
        group.Title,
        answer => ((BaseCategoryItem) group.Parent).ValidateNewGroupTitle(answer),
        out var output);

      if (!result) return;
      parent.GroupRename(group, output);
    }

    private static void CategoryGroupDelete(object parameter) {
      if (!(parameter is CategoryGroup group) || !(group.Parent is BaseCategoryItem parent)) return;
      if (!MessageDialog.Show(
        "Delete Confirmation", 
        $"Do you really want to delete '{group.Title}' group?", 
        true)) return;
      parent.GroupDelete(group);
    }

    private static void TagItemNew(object parameter) {
      if (!(parameter is BaseTreeViewItem item) || !(item.GetTopParent() is ICategoryItem parent)) return;

      var result = InputDialog.Open(
        parent.CategoryGroupIconName,
        "New Item",
        "Enter the name of the new Item.",
        string.Empty,
        answer => parent.ValidateNewItemTitle(item, answer),
        out var output);

      if (!result) return;
      parent.ItemCreate(item, output);
    }

    private static void TagItemRename(object parameter) {
      if (!(parameter is BaseTreeViewItem item) || !(item.GetTopParent() is ICategoryItem parent)) return;

      var result = InputDialog.Open(
        parent.CategoryGroupIconName,
        "Rename Item",
        "Enter the new name for the Item.",
        item.Title,
        answer => parent.ValidateNewItemTitle(item.Parent, answer),
        out var output);

      if (!result) return;
      parent.ItemRename(item, output);
    }

    private static void TagItemDelete(object parameter) {
      if (!(parameter is BaseTreeViewItem item) || !(item.GetTopParent() is ICategoryItem parent)) return;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you really want to delete '{item.Title}'?", true)) return;
      parent.ItemDelete(item);
    }

    private static void TagItemDeleteNotUsed(object parameter) {
      if (!(parameter is BaseTreeViewItem item)) return;
      if (!(item.GetTopParent() is BaseCategoryItem topParent)) return;

      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete not used items in '{item.Title}'?", true)) return;

      switch (topParent.Category) {
        case Category.People: {
          foreach (var person in item.Items.Cast<Person>().Where(x => x.MediaItems.Count == 0).ToArray())
            ((ICategoryItem) topParent).ItemDelete(person);

          break;
        }
        case Category.Keywords: {
          foreach (var keyword in item.Items.Cast<Keyword>().Where(x => x.MediaItems.Count == 0).ToArray())
            ((ICategoryItem) topParent).ItemDelete(keyword);

          break;
        }
      }
    }

    private static void ViewerIncludeFolder(object parameter) {
      ViewersViewModel.AddFolder((Viewer) parameter, true);
    }

    private static void ViewerExcludeFolder(object parameter) {
      ViewersViewModel.AddFolder((Viewer) parameter, false);
    }

    private static void ViewerRemoveFolder(object parameter) {
      var folder = (BaseTreeViewItem)parameter;
      folder.Parent?.Items.Remove(folder);
      App.Core.Model.Viewers.Helper.Table.SaveToFile();
    }

    private static void FolderNew(object parameter) {
      var folder = (Folder) parameter;
      var result = InputDialog.Open(
        IconName.Folder,
        "New Folder",
        "Enter the name of the new folder.",
        string.Empty,
        answer => folder.ValidateNewFolderName(answer, false),
        out var output);

      if (!result) return;
      
      try {
        folder.New(output);
      }
      catch (Exception ex) {
        ErrorDialog.Show(ex);
      }
      
      App.Core.Model.Sdb.SaveAllTables();
      if (folder.IsFolderKeyword || folder.FolderKeyword != null)
        App.Core.Model.FolderKeywords.Load();
    }

    private static void FolderRename(object parameter) {
      var folder = (Folder) parameter;
      var result = InputDialog.Open(
        IconName.Folder,
        "Rename Folder",
        "Enter the new name for the folder.",
        folder.Title,
        answer => folder.ValidateNewFolderName(answer, true),
        out var output);

      if (!result) return;

      try {
        folder.Rename(output);
      }
      catch (Exception ex) {
        ErrorDialog.Show(ex);
      }

      App.Core.Model.Sdb.SaveAllTables();
      if (folder.IsFolderKeyword || folder.FolderKeyword != null)
        App.Core.Model.FolderKeywords.Load();

      // reload if the folder was selected before
      if (folder.IsSelected)
        App.Core.TreeView_Select(folder, false, false, false);
    }

    private static void FolderDelete(object parameter) {
      var folder = (Folder)parameter;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you really want to delete '{folder.Title}' folder?", true)) return;

      // delete folder, subfolders and mediaItems from file system
      if (Directory.Exists(folder.FullPath))
        AppCore.FileOperationDelete(new List<string> { folder.FullPath }, true, false);

      App.Core.Model.Folders.DeleteRecord(folder);
      App.Core.Model.FolderKeywords.Load();
    }

    private static void FolderAddToFavorites(object parameter) {
      App.Core.Model.FavoriteFolders.Add((Folder)parameter);
    }

    private static void FolderRemoveFromFavorites(object parameter) {
      App.Core.Model.FavoriteFolders.Remove((FavoriteFolder)parameter);
    }

    private static void FolderSetAsFolderKeyword(object parameter) {
      ((Folder)parameter).IsFolderKeyword = true;
      App.Core.Model.Folders.Helper.Table.SaveToFile();
      App.Core.Model.FolderKeywords.Load();
    }

    private static void GeoNameNew(object parameter) {
      if (!GeoNamesViewModel.IsGeoNamesUserNameInSettings()) return;

      var result = InputDialog.Open(
        IconName.LocationCheckin,
        "GeoName latitude and longitude",
        "Enter in format: N36.75847,W3.84609",
        string.Empty,
        answer => null,
        out var output);

      if (!result) return;
      ((GeoNames) parameter).New(output, Settings.Default.GeoNamesUserName);
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
