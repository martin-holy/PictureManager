using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.ViewModels;
using System.Linq;
using System.Windows.Input;
using PictureManager.ViewModels.Tree;

namespace PictureManager.Commands {
  public static class TreeViewCommands {
    public static RoutedUICommand TagItemDeleteNotUsedCommand { get; } = new() { Text = "Delete not used" };
    public static RoutedUICommand FolderAddToFavoritesCommand { get; } = new() { Text = "Add to Favorites" };
    public static RoutedUICommand FolderSetAsFolderKeywordCommand { get; } = new() { Text = "Set as Folder Keyword" };
    public static RoutedUICommand GeoNameNewCommand { get; } = new() { Text = "New" };
    public static RoutedUICommand ActivateFilterAndCommand { get; } = new() { Text = "Filter And" };
    public static RoutedUICommand ActivateFilterOrCommand { get; } = new() { Text = "Filter Or" };
    public static RoutedUICommand ActivateFilterNotCommand { get; } = new() { Text = "Filter Not" };
    public static RoutedUICommand LoadByTagCommand { get; } = new() { Text = "Load" };

    public static void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, TagItemDeleteNotUsedCommand, TagItemDeleteNotUsed);
      CommandsController.AddCommandBinding(cbc, FolderAddToFavoritesCommand, FolderAddToFavorites);
      CommandsController.AddCommandBinding(cbc, FolderSetAsFolderKeywordCommand, FolderSetAsFolderKeyword);
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
        case Category.People: 
          App.Core.PeopleM.DeleteNotUsed(item.Items.OfType<PersonTreeVM>().Select(x => x.BaseVM.Model));
          break;

        case Category.Keywords:
          App.Core.KeywordsM.DeleteNotUsed(item.Items.OfType<KeywordTreeVM>().Select(x => x.BaseVM.Model));
          break;
      }
    }

    private static void FolderAddToFavorites(object parameter) => App.Core.FavoriteFoldersM.ItemCreate((Folder)parameter);

    private static void FolderSetAsFolderKeyword(object parameter) {
      ((Folder)parameter).IsFolderKeyword = true;
      App.Core.Folders.DataAdapter.IsModified = true;
      App.Core.FolderKeywords.Load();
    }

    private static void GeoNameNew(object parameter) {
      if (!GeoNamesViewModel.IsGeoNamesUserNameInSettings()) return;

      var result = InputDialog.Open(
        IconName.LocationCheckin,
        "GeoName latitude and longitude",
        "Enter in format: N36.75847,W3.84609",
        string.Empty,
        _ => null,
        out var output);

      if (!result) return;
      ((GeoNamesM)parameter).New(output, Settings.Default.GeoNamesUserName);
    }

    private static async void ActivateFilterAnd(object parameter) => await App.Ui.ActivateFilter((ICatTreeViewItem)parameter, BackgroundBrush.AndThis);

    private static async void ActivateFilterOr(object parameter) => await App.Ui.ActivateFilter((ICatTreeViewItem)parameter, BackgroundBrush.OrThis);

    private static async void ActivateFilterNot(object parameter) => await App.Ui.ActivateFilter((ICatTreeViewItem)parameter, BackgroundBrush.Hidden);

    private static void LoadByTag(object parameter) =>
      _ = App.Ui.TreeView_Select((ICatTreeViewItem)parameter,
        (Keyboard.Modifiers & ModifierKeys.Control) > 0,
        (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
        (Keyboard.Modifiers & ModifierKeys.Shift) > 0, true);
  }
}
