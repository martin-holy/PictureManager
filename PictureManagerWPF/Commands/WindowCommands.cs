﻿using System.Linq;
using System.Windows;
using System.Windows.Input;
using PictureManager.Database;
using PictureManager.Dialogs;
using PictureManager.Patterns;
using PictureManager.Properties;

namespace PictureManager.Commands {
  public class WindowCommands: SingletonBase<WindowCommands> {
    public static RoutedUICommand SwitchToFullScreenCommand { get; } = new RoutedUICommand();
    public static RoutedUICommand SwitchToBrowserCommand { get; } = CommandsController.CreateCommand("Switch to Browser", "SwitchToBrowser", new KeyGesture(Key.Escape));
    public static RoutedUICommand TestButtonCommand { get; } = CommandsController.CreateCommand("Test Button", "TestButton", new KeyGesture(Key.D, ModifierKeys.Control));
    public static RoutedUICommand OpenSettingsCommand { get; } = new RoutedUICommand { Text = "Settings" };
    public static RoutedUICommand OpenAboutCommand { get; } = new RoutedUICommand { Text = "About" };
    public static RoutedUICommand ShowHideTabMainCommand { get; } = CommandsController.CreateCommand("S/H", "ShowHideTabMain", new KeyGesture(Key.T, ModifierKeys.Control));
    public static RoutedUICommand AddGeoNamesFromFilesCommand { get; } = new RoutedUICommand { Text = "GeoNames" };
    public static RoutedUICommand ViewerChangeCommand { get; } = new RoutedUICommand { Text = "" };
    public static RoutedUICommand OpenFolderKeywordsListCommand { get; } = new RoutedUICommand { Text = "Folder Keyword List" };
    public static RoutedUICommand OpenLogCommand { get; } = new RoutedUICommand { Text = "Log" };

    private bool _mainTreeViewIsPinnedInViewer;
    private bool _mainTreeViewIsPinnedInBrowser = true;

    public void AddCommandBindings(CommandBindingCollection cbc) {
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
    }

    private static bool CanSwitchToFullScreen() {
      return App.Core.AppInfo.AppMode == AppMode.Browser;
    }

    public void SwitchToFullScreen() {
      if (App.Core.MediaItems.Current == null) return;
      App.Core.AppInfo.AppMode = AppMode.Viewer;
      ShowHideTabMain(_mainTreeViewIsPinnedInViewer);
      App.WMain.UseNoneWindowStyle = true;
      App.WMain.IgnoreTaskbarOnMaximize = true;
      App.WMain.MainMenu.Visibility = Visibility.Hidden;
    }

    private static bool CanSwitchToBrowser() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer;
    }

    public void SwitchToBrowser() {
      App.WMain.PresentationPanel.Stop();
      App.Core.AppInfo.AppMode = AppMode.Browser;
      ShowHideTabMain(_mainTreeViewIsPinnedInBrowser);
      App.Core.MediaItems.SplittedItemsReload();
      App.Core.MediaItems.ScrollToCurrent();
      App.Core.MarkUsedKeywordsAndPeople();
      App.WMain.UseNoneWindowStyle = false;
      App.WMain.ShowTitleBar = true;
      App.WMain.IgnoreTaskbarOnMaximize = false;
      App.WMain.MainMenu.Visibility = Visibility.Visible;
      App.WMain.FullImage.SetSource(null);
      App.WMain.FullImage.Stop();
      App.WMain.FullMedia.MediaElement.Source = null;
      App.WMain.FullMedia.IsPlaying = false;
    }

    private void OpenSettings() {
      var settings = new SettingsDialog { Owner = App.WMain };
      if (settings.ShowDialog() ?? true)
        Settings.Default.Save();
      else
        Settings.Default.Reload();
    }

    private void OpenAbout() {
      var about = new AboutDialog { Owner = App.WMain };
      about.ShowDialog();
    }

    private void ShowHideTabMain(object parameter) {
      var show = false;
      var reload = false;
      if (parameter != null)
        show = (bool)parameter;
      else {
        switch (App.Core.AppInfo.AppMode) {
          case AppMode.Browser:
            reload = true;
            _mainTreeViewIsPinnedInBrowser = !_mainTreeViewIsPinnedInBrowser;
            show = _mainTreeViewIsPinnedInBrowser;
            break;
          case AppMode.Viewer:
            _mainTreeViewIsPinnedInViewer = !_mainTreeViewIsPinnedInViewer;
            show = _mainTreeViewIsPinnedInViewer;
            break;
        }
      }

      if (show) {
        App.WMain.GridMain.ColumnDefinitions[0].Width = new GridLength(App.WMain.FlyoutMainTreeView.ActualWidth);
        App.WMain.GridMain.ColumnDefinitions[1].Width = new GridLength(3);
      }
      else {
        App.WMain.GridMain.ColumnDefinitions[0].Width = new GridLength(0);
        App.WMain.GridMain.ColumnDefinitions[1].Width = new GridLength(0);
      }

      App.WMain.FlyoutMainTreeView.IsPinned = show;
      App.WMain.FlyoutMainTreeView.IsOpen = show;

      App.WMain.SetFlyoutMainTreeViewMargin();

      if (reload) {
        App.Core.MediaItems.SplittedItemsReload();
        App.Core.MediaItems.ScrollToCurrent();
      }
    }

    private void OpenFolderKeywordsList() {
      var fkl = new FolderKeywordList { Owner = App.WMain };
      fkl.ShowDialog();
    }

    private static bool CanAddGeoNamesFromFiles() {
      return App.Core.MediaItems.FilteredItems.Count(x => x.IsSelected) > 0;
    }

    private void AddGeoNamesFromFiles() {
      if (!GeoNames.AreSettingsSet()) return;

      var progress = new ProgressBarDialog(App.WMain, true, 1, "Adding GeoNames ...");
      progress.AddEvents(
        App.Core.MediaItems.FilteredItems.Where(x => x.IsSelected).ToArray(),
        null,
        // action
        delegate (MediaItem mi) {
          if (mi.Lat == null || mi.Lng == null) mi.ReadMetadata(true);
          if (mi.Lat == null || mi.Lng == null) return;

          var lastGeoName = App.Core.GeoNames.InsertGeoNameHierarchy((double)mi.Lat, (double)mi.Lng);
          if (lastGeoName == null) return;

          mi.GeoName = lastGeoName;
          mi.TryWriteMetadata();
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.Sdb.SaveAllTables();

          var info = App.Core.AppInfo;
          info.FullGeoName = info.CurrentMediaItem?.GeoName?.GetFullPath("\n");
          info.OnPropertyChanged(nameof(info.IsGeoNameVisible));
        });

      progress.StartDialog();
    }

    private void ViewerChange(object parameter) {
      if (App.Core.CurrentViewer != null)
        App.Core.CurrentViewer.IsDefault = false;

      var viewer = (Viewer)parameter;
      viewer.IsDefault = true;
      App.Core.Viewers.Helper.Table.SaveToFile();

      App.WMain.MenuViewers.Header = viewer.Title;
      App.Core.CurrentViewer = viewer;
      App.Core.Folders.AddDrives();
      App.Core.FolderKeywords.Load();
    }

    private void OpenLog() {
      var log = new LogDialog { Owner = App.WMain };
      log.ShowDialog();
    }

    private static void TestButton() {
      var tests = new Tests();
      tests.Run();
    }
  }
}
