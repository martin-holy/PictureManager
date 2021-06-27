using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Patterns;
using PictureManager.Properties;
using PictureManager.ViewModels;
using System.Linq;
using System.Windows.Input;

namespace PictureManager.Commands {
  public class TreeViewCommands : Singleton<TreeViewCommands> {
    public static RoutedUICommand TagItemDeleteNotUsedCommand { get; } = new RoutedUICommand { Text = "Delete not used" };
    public static RoutedUICommand FolderAddToFavoritesCommand { get; } = new RoutedUICommand { Text = "Add to Favorites" };
    public static RoutedUICommand FolderSetAsFolderKeywordCommand { get; } = new RoutedUICommand { Text = "Set as Folder Keyword" };
    public static RoutedUICommand ViewerIncludeFolderCommand { get; } = new RoutedUICommand { Text = "Include for Viewer" };
    public static RoutedUICommand ViewerExcludeFolderCommand { get; } = new RoutedUICommand { Text = "Exclude for Viewer" };
    public static RoutedUICommand GeoNameNewCommand { get; } = new RoutedUICommand { Text = "New" };
    public static RoutedUICommand ActivateFilterAndCommand { get; } = new RoutedUICommand { Text = "Filter And" };
    public static RoutedUICommand ActivateFilterOrCommand { get; } = new RoutedUICommand { Text = "Filter Or" };
    public static RoutedUICommand ActivateFilterNotCommand { get; } = new RoutedUICommand { Text = "Filter Not" };
    public static RoutedUICommand LoadByTagCommand { get; } = new RoutedUICommand { Text = "Load" };

    public static void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, TagItemDeleteNotUsedCommand, TagItemDeleteNotUsed);
      CommandsController.AddCommandBinding(cbc, FolderAddToFavoritesCommand, FolderAddToFavorites);
      CommandsController.AddCommandBinding(cbc, FolderSetAsFolderKeywordCommand, FolderSetAsFolderKeyword);
      CommandsController.AddCommandBinding(cbc, ViewerIncludeFolderCommand, ViewerIncludeFolder);
      CommandsController.AddCommandBinding(cbc, ViewerExcludeFolderCommand, ViewerExcludeFolder);
      CommandsController.AddCommandBinding(cbc, GeoNameNewCommand, GeoNameNew);
      CommandsController.AddCommandBinding(cbc, ActivateFilterAndCommand, ActivateFilterAnd);
      CommandsController.AddCommandBinding(cbc, ActivateFilterOrCommand, ActivateFilterOr);
      CommandsController.AddCommandBinding(cbc, ActivateFilterNotCommand, ActivateFilterNot);
      CommandsController.AddCommandBinding(cbc, LoadByTagCommand, LoadByTag);
    }

    private static void TagItemDeleteNotUsed(object parameter) {
      if (parameter is not ICatTreeViewItem item || CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat) return;

      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete not used items in '{item.Title}'?", true)) return;

      switch (cat.Category) {
        case Category.People: {
          foreach (var person in item.Items.Cast<Person>().Where(x => x.MediaItems.Count == 0).ToArray())
            cat.ItemDelete(person);

          break;
        }
        case Category.Keywords: {
          foreach (var keyword in item.Items.Cast<Keyword>().Where(x => x.MediaItems.Count == 0).ToArray())
            cat.ItemDelete(keyword);

          break;
        }
      }
    }

    private static void ViewerIncludeFolder(object parameter) => ViewersViewModel.AddFolder((Viewer)parameter, true);

    private static void ViewerExcludeFolder(object parameter) => ViewersViewModel.AddFolder((Viewer)parameter, false);

    private static void FolderAddToFavorites(object parameter) => App.Core.FavoriteFolders.ItemCreate((Folder)parameter);

    private static void FolderSetAsFolderKeyword(object parameter) {
      ((Folder)parameter).IsFolderKeyword = true;
      App.Db.SetModified<Folders>();
      App.Core.FolderKeywords.Load();
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
      ((GeoNames)parameter).New(output, Settings.Default.GeoNamesUserName);
    }

    private static void ActivateFilterAnd(object parameter) => App.Ui.ActivateFilter((ICatTreeViewItem)parameter, BackgroundBrush.AndThis);

    private static void ActivateFilterOr(object parameter) => App.Ui.ActivateFilter((ICatTreeViewItem)parameter, BackgroundBrush.OrThis);

    private static void ActivateFilterNot(object parameter) => App.Ui.ActivateFilter((ICatTreeViewItem)parameter, BackgroundBrush.Hidden);

    private static void LoadByTag(object parameter) =>
      App.Ui.TreeView_Select((ICatTreeViewItem)parameter,
        (Keyboard.Modifiers & ModifierKeys.Control) > 0,
        (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
        (Keyboard.Modifiers & ModifierKeys.Shift) > 0, true);
  }
}
