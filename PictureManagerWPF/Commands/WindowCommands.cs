using PictureManager.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Commands {
  public static class WindowCommands {
    public static RoutedUICommand SwitchToFullScreenCommand { get; } = new();
    public static RoutedUICommand SwitchToBrowserCommand { get; } = CommandsController.CreateCommand("Switch to Browser", "SwitchToBrowser", new KeyGesture(Key.Escape));
    public static RoutedUICommand TestButtonCommand { get; } = CommandsController.CreateCommand("Test Button", "TestButton", new KeyGesture(Key.D, ModifierKeys.Control));
    public static RoutedUICommand OpenSettingsCommand { get; } = new() { Text = "Settings" };
    public static RoutedUICommand OpenAboutCommand { get; } = new() { Text = "About" };
    public static RoutedUICommand ShowHideTabMainCommand { get; } = CommandsController.CreateCommand("S/H", "ShowHideTabMain", new KeyGesture(Key.T, ModifierKeys.Control));
    public static RoutedUICommand AddGeoNamesFromFilesCommand { get; } = new() { Text = "GeoNames" };
    public static RoutedUICommand ViewerChangeCommand { get; } = new() { Text = "" };
    public static RoutedUICommand OpenFolderKeywordsListCommand { get; } = new() { Text = "Folder Keyword List" };
    public static RoutedUICommand OpenLogCommand { get; } = new() { Text = "Log" };
    public static RoutedUICommand SaveDbCommand { get; } = new() { Text = "DB" };

    private static bool _mainTreeViewIsPinnedInViewer;
    private static bool _mainTreeViewIsPinnedInBrowser = true;

    public static void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, SwitchToFullScreenCommand, SwitchToFullScreen, CanSwitchToFullScreen);
      CommandsController.AddCommandBinding(cbc, SwitchToBrowserCommand, SwitchToBrowser, CanSwitchToBrowser);
      CommandsController.AddCommandBinding(cbc, TestButtonCommand, TestButton);
      CommandsController.AddCommandBinding(cbc, OpenSettingsCommand, OpenSettings);
      CommandsController.AddCommandBinding(cbc, AddGeoNamesFromFilesCommand, AddGeoNamesFromFiles, CanAddGeoNamesFromFiles);
      CommandsController.AddCommandBinding(cbc, ViewerChangeCommand, ViewerChange);
      CommandsController.AddCommandBinding(cbc, OpenAboutCommand, OpenAbout);
      CommandsController.AddCommandBinding(cbc, OpenFolderKeywordsListCommand, OpenFolderKeywordsList);
      CommandsController.AddCommandBinding(cbc, ShowHideTabMainCommand, ShowHideTabMain);
      CommandsController.AddCommandBinding(cbc, OpenLogCommand, OpenLog);
      CommandsController.AddCommandBinding(cbc, SaveDbCommand, SaveDb, CanSaveDb);
    }

    private static bool CanSwitchToFullScreen() => App.Ui.AppInfo.AppMode == AppMode.Browser;

    public static void SwitchToFullScreen() {
      if (App.Core.MediaItems.Current == null) return;
      App.Ui.AppInfo.AppMode = AppMode.Viewer;
      ShowHideTabMain(_mainTreeViewIsPinnedInViewer);
      App.WMain.UseNoneWindowStyle = true;
      App.WMain.IgnoreTaskbarOnMaximize = true;
      App.WMain.MainMenu.Visibility = Visibility.Hidden;
    }

    private static bool CanSwitchToBrowser() => App.Ui.AppInfo.AppMode == AppMode.Viewer;

    public static void SwitchToBrowser() {
      App.WMain.UseNoneWindowStyle = false;
      App.WMain.ShowTitleBar = true;
      App.WMain.IgnoreTaskbarOnMaximize = false;
      App.WMain.MainMenu.Visibility = Visibility.Visible;

      App.Ui.AppInfo.AppMode = AppMode.Browser;
      ShowHideTabMain(_mainTreeViewIsPinnedInBrowser);
      App.Ui.MediaItemsViewModel.ScrollToCurrent();
      App.Ui.MarkUsedKeywordsAndPeople();

      App.WMain.MediaViewer.Deactivate();
    }

    private static void OpenSettings() {
      var settings = new SettingsDialog { Owner = App.WMain };
      if (settings.ShowDialog() ?? true)
        Settings.Default.Save();
      else
        Settings.Default.Reload();
    }

    private static void OpenAbout() {
      var about = new AboutDialog { Owner = App.WMain };
      about.ShowDialog();
    }

    private async static void ShowHideTabMain(object parameter) {
      var show = false;
      var reload = false;
      if (parameter != null)
        show = (bool)parameter;
      else {
        switch (App.Ui.AppInfo.AppMode) {
          case AppMode.Browser: {
            reload = true;
            _mainTreeViewIsPinnedInBrowser = !_mainTreeViewIsPinnedInBrowser;
            show = _mainTreeViewIsPinnedInBrowser;
            break;
          }
          case AppMode.Viewer: {
            _mainTreeViewIsPinnedInViewer = !_mainTreeViewIsPinnedInViewer;
            show = _mainTreeViewIsPinnedInViewer;
            break;
          }
        }
      }

      App.WMain.SlidePanelMainTreeView.IsPinned = show;
      App.WMain.SlidePanelMainTreeView.IsOpen = show;

      if (reload)
        await App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
    }

    private static void OpenFolderKeywordsList() {
      var fkl = new FolderKeywordList { Owner = App.WMain };
      fkl.ShowDialog();
    }

    private static bool CanAddGeoNamesFromFiles() => App.Core.MediaItems.ThumbsGrid?.FilteredItems.Count(x => x.IsSelected) > 0;

    private static void AddGeoNamesFromFiles() {
      if (!GeoNamesViewModel.IsGeoNamesUserNameInSettings()) return;

      var progress = new ProgressBarDialog(App.WMain, true, 1, "Adding GeoNames ...");
      progress.AddEvents(
        App.Core.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToArray(),
        null,
        // action
        async (MediaItem mi) => {
          if (mi.Lat == null || mi.Lng == null) _ = await MediaItemsViewModel.ReadMetadata(mi, true);
          if (mi.Lat == null || mi.Lng == null) return;

          var lastGeoName = App.Core.GeoNames.InsertGeoNameHierarchy((double)mi.Lat, (double)mi.Lng, Settings.Default.GeoNamesUserName);
          if (lastGeoName == null) return;

          mi.GeoName = lastGeoName;
          MediaItemsViewModel.TryWriteMetadata(mi);
          await App.Core.RunOnUiThread(() => {
            mi.SetInfoBox();
            App.Core.MediaItems.DataAdapter.IsModified = true;
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.MediaItems.Current?.GeoName?.OnPropertyChanged(nameof(App.Core.MediaItems.Current.GeoName.FullGeoName));
        });

      progress.StartDialog();
    }

    private static void ViewerChange(object parameter) {
      if (App.Core.CurrentViewer != null)
        App.Core.CurrentViewer.IsDefault = false;

      var viewer = (Viewer)parameter;
      viewer.IsDefault = true;
      App.Core.Viewers.DataAdapter.Save();

      App.WMain.MenuViewers.Header = viewer.Title;
      App.Core.CurrentViewer = viewer;
      App.Core.Folders.AddDrives();
      App.Core.FolderKeywords.Load();
      viewer.UpdateHashSets();
      App.Ui.ViewersBaseVM.UpdateCategoryGroupsVisibility(viewer);
    }

    private static void OpenLog() {
      var log = new LogDialog { Owner = App.WMain };
      log.ShowDialog();
    }

    private static void TestButton() => Tests.Run();

    private static bool CanSaveDb() => App.Db.Changes > 0;

    private static void SaveDb() => App.Db.SaveAllTables();
  }
}
