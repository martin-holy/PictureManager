using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PictureManager.Dialogs;
using PictureManager.Properties;

namespace PictureManager {
  public partial class WMain {

    private void AddCommandBindings() {
      //Window Commands
      CommandBindings.Add(new CommandBinding(Commands.SwitchToFullScreen, HandleExecute(SwitchToFullScreen)));
      CommandBindings.Add(new CommandBinding(Commands.SwitchToBrowser, HandleExecute(SwitchToBrowser)));
      //MediaItems Commands
      CommandBindings.Add(new CommandBinding(Commands.MediaItemNext, HandleExecute(MediaItemNext), HandleCanExecute(CanMediaItemNext)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemPrevious, HandleExecute(MediaItemPrevious), HandleCanExecute(CanMediaItemPrevious)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsSelectAll, HandleExecute(MediaItemsSelectAll), HandleCanExecute(CanMediaItemsSelectAll)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsSelectNotModifed, HandleExecute(MediaItemsSelectNotModifed), HandleCanExecute(CanMediaItemsSelectNotModifed)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsDelete, HandleExecute(MediaItemsDelete), HandleCanExecute(CanMediaItemsDelete)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsLoadByTag, HandleExecute(MediaItemsLoadByTag)));
      CommandBindings.Add(new CommandBinding(Commands.Presentation, HandleExecute(Presentation), HandleCanExecute(CanPresentation)));
      //TreeView Commands
      CommandBindings.Add(new CommandBinding(Commands.CategoryGroupNew, HandleExecute(CategoryGroupNew)));
      CommandBindings.Add(new CommandBinding(Commands.CategoryGroupRename, HandleExecute(CategoryGroupRename)));
      CommandBindings.Add(new CommandBinding(Commands.CategoryGroupDelete, HandleExecute(CategoryGroupDelete)));
      CommandBindings.Add(new CommandBinding(Commands.TagItemNew, HandleExecute(TagItemNew)));
      CommandBindings.Add(new CommandBinding(Commands.TagItemRename, HandleExecute(TagItemRename)));
      CommandBindings.Add(new CommandBinding(Commands.TagItemDelete, HandleExecute(TagItemDelete)));
      CommandBindings.Add(new CommandBinding(Commands.FolderNew, HandleExecute(FolderNew)));
      CommandBindings.Add(new CommandBinding(Commands.FolderRename, HandleExecute(FolderRename)));
      CommandBindings.Add(new CommandBinding(Commands.FolderDelete, HandleExecute(FolderDelete)));
      CommandBindings.Add(new CommandBinding(Commands.FolderAddToFavorites, HandleExecute(FolderAddToFavorites)));
      CommandBindings.Add(new CommandBinding(Commands.FolderRemoveFromFavorites, HandleExecute(FolderRemoveFromFavorites)));
      CommandBindings.Add(new CommandBinding(Commands.FolderSetAsFolderKeyword, HandleExecute(FolderSetAsFolderKeyword)));
      CommandBindings.Add(new CommandBinding(Commands.ViewerIncludeFolder, HandleExecute(ViewerIncludeFolder)));
      CommandBindings.Add(new CommandBinding(Commands.ViewerExcludeFolder, HandleExecute(ViewerExcludeFolder)));
      CommandBindings.Add(new CommandBinding(Commands.ViewerRemoveFolder, HandleExecute(ViewerRemoveFolder)));
      CommandBindings.Add(new CommandBinding(Commands.GeoNameNew, HandleExecute(GeoNameNew)));
      //Menu Commands
      CommandBindings.Add(new CommandBinding(Commands.KeywordsEdit, HandleExecute(KeywordsEdit), HandleCanExecute(CanKeywordsEdit)));
      CommandBindings.Add(new CommandBinding(Commands.KeywordsSave, HandleExecute(KeywordsSave), HandleCanExecute(CanKeywordsSave)));
      CommandBindings.Add(new CommandBinding(Commands.KeywordsCancel, HandleExecute(KeywordsCancel), HandleCanExecute(CanKeywordsCancel)));
      CommandBindings.Add(new CommandBinding(Commands.KeywordsComment, HandleExecute(KeywordsComment), HandleCanExecute(CanKeywordsComment)));
      CommandBindings.Add(new CommandBinding(Commands.CompressPictures, HandleExecute(CompressPictures), HandleCanExecute(CanCompressPictures)));
      CommandBindings.Add(new CommandBinding(Commands.TestButton, HandleExecute(TestButton)));
      CommandBindings.Add(new CommandBinding(Commands.ReloadMetadata, HandleExecute(ReloadMetadata)));
      CommandBindings.Add(new CommandBinding(Commands.RebuildThumbnails, HandleExecute(RebuildThumbnails)));
      CommandBindings.Add(new CommandBinding(Commands.OpenSettings, HandleExecute(OpenSettings)));
      CommandBindings.Add(new CommandBinding(Commands.AddGeoNamesFromFiles, HandleExecute(AddGeoNamesFromFiles), HandleCanExecute(CanAddGeoNamesFromFiles)));
      CommandBindings.Add(new CommandBinding(Commands.ViewerChange, HandleExecute(ViewerChange)));
      CommandBindings.Add(new CommandBinding(Commands.OpenAbout, HandleExecute(OpenAbout)));
      CommandBindings.Add(new CommandBinding(Commands.OpenFolderKeywordsList, HandleExecute(OpenFolderKeywordsList)));
      CommandBindings.Add(new CommandBinding(Commands.ShowHideTabMain, HandleExecute(ShowHideTabMain)));
    }

    private void AddInputBindings() {
      AddInputBinding(Commands.ShowHideTabMain, new KeyGesture(Key.T, ModifierKeys.Control));
      AddInputBinding(Commands.SwitchToBrowser, new KeyGesture(Key.Escape), PanelFullScreen);
      AddInputBinding(Commands.MediaItemNext, new KeyGesture(Key.Right), PanelFullScreen);
      AddInputBinding(Commands.MediaItemPrevious, new KeyGesture(Key.Left), PanelFullScreen);
      AddInputBinding(Commands.MediaItemsSelectAll, new KeyGesture(Key.A, ModifierKeys.Control), ThumbsBox);
      AddInputBinding(Commands.MediaItemsDelete, new KeyGesture(Key.Delete), PanelFullScreen);
      AddInputBinding(Commands.MediaItemsDelete, new KeyGesture(Key.Delete, ModifierKeys.Shift), PanelFullScreen);
      AddInputBinding(Commands.Presentation, new KeyGesture(Key.P, ModifierKeys.Control), PanelFullScreen);
      AddInputBinding(MediaCommands.TogglePlayPause, new KeyGesture(Key.Space), FullMedia);
      AddInputBinding(MediaCommands.TogglePlayPause, new MouseGesture(MouseAction.LeftClick), FullMedia);
      
      AddInputBinding(Commands.KeywordsEdit, new KeyGesture(Key.E, ModifierKeys.Control));
      AddInputBinding(Commands.KeywordsSave, new KeyGesture(Key.S, ModifierKeys.Control));
      AddInputBinding(Commands.KeywordsCancel, new KeyGesture(Key.Q, ModifierKeys.Control));
      AddInputBinding(Commands.KeywordsComment, new KeyGesture(Key.K, ModifierKeys.Control));

      AddInputBinding(Commands.TestButton, new KeyGesture(Key.D, ModifierKeys.Control));
    }

    private void AddInputBinding(ICommand command, InputGesture gesture, IInputElement commandTarget = null) {
      InputBindings.Add(new InputBinding(command, gesture) {CommandTarget = commandTarget});
    }

    private static ExecutedRoutedEventHandler HandleExecute(Action action) {
      return (o, e) => {
        action();
        e.Handled = true;
      };
    }

    private static ExecutedRoutedEventHandler HandleExecute(Action<object> action) {
      return (o, e) => {
        action(e.Parameter);
        e.Handled = true;
      };
    }

    private static CanExecuteRoutedEventHandler HandleCanExecute(Func<bool> canExecute) {
      return (o, e) => {
        e.CanExecute = canExecute();
        e.Handled = true;
      };
    }

    /*private static CanExecuteRoutedEventHandler HandleCanExecute(Func<object, bool> canExecute) {
      return (o, e) => {
        e.CanExecute = canExecute(e.Parameter);
        e.Handled = true;
      };
    }*/

    private bool CanMediaItemNext() {
      return ACore.AppInfo.AppMode == AppMode.Viewer && ACore.MediaItems.Current?.Index + 1 < ACore.MediaItems.Items.Count;
    }

    private void MediaItemNext() {
      var current = ACore.MediaItems.Items[ACore.MediaItems.Current.Index + 1];
      ACore.MediaItems.Current = current;
      SetMediaItemSource();
      if (_presentationTimer.Enabled && (current.MediaType == MediaType.Video || current.IsPanoramatic)) {
        _presentationTimer.Enabled = false;
        _presentationTimerPaused = true;

        if (current.MediaType == MediaType.Image && current.IsPanoramatic)
          FullImage.Play(PresentationInterval, delegate { StartPresentationTimer(false); });
      }

      ACore.MarkUsedKeywordsAndPeople();
      ACore.UpdateStatusBarInfo();
    }

    private bool CanMediaItemPrevious() {
      return ACore.AppInfo.AppMode == AppMode.Viewer && ACore.MediaItems.Current?.Index > 0;
    }

    private void MediaItemPrevious() {
      ACore.MediaItems.Current = ACore.MediaItems.Items[ACore.MediaItems.Current.Index - 1];
      SetMediaItemSource();
      ACore.MarkUsedKeywordsAndPeople();
      ACore.UpdateStatusBarInfo();
    }

    private bool CanMediaItemsSelectAll() {
      return ACore.AppInfo.AppMode == AppMode.Browser && ACore.MediaItems.Items.Count > 0;
    }

    private void MediaItemsSelectAll() {
      ACore.MediaItems.SelectAll();
      ACore.UpdateStatusBarInfo();
      ACore.MarkUsedKeywordsAndPeople();
    }

    private bool CanMediaItemsSelectNotModifed() {
      return ACore.AppInfo.AppMode == AppMode.Browser && ACore.MediaItems.Items.Count > 0;
    }

    private void MediaItemsSelectNotModifed() {
      ACore.MediaItems.SelectNotModifed();
      ACore.UpdateStatusBarInfo();
      ACore.MarkUsedKeywordsAndPeople();
    }

    private bool CanMediaItemsDelete() {
      return ACore.AppInfo.Selected > 0;
    }

    private void MediaItemsDelete() {
      var count = ACore.MediaItems.Items.Count(x => x.IsSelected);
      if (!MessageDialog.Show("Delete Confirmation", 
        $"Do you realy want to delete {count} item{(count > 1 ? "s" : string.Empty)}?", true)) return;

      ACore.MediaItems.RemoveSelected(true);

      if (ACore.AppInfo.AppMode == AppMode.Viewer) {
        if (ACore.MediaItems.Current != null)
          SetMediaItemSource();
        else
          SwitchToBrowser();
      }

      ACore.UpdateStatusBarInfo();
    }

    private void MediaItemsLoadByTag(object parameter) {
      ACore.MediaItems.LoadByTag((ViewModel.BaseTreeViewTagItem) parameter, (Keyboard.Modifiers & ModifierKeys.Shift) > 0);
    }

    private bool CanPresentation() {
      return ACore.AppInfo.AppMode == AppMode.Viewer && ACore.MediaItems.Current != null;
    }

    private void Presentation() {
      if (FullImage.IsAnimationOn) {
        FullImage.Stop();
        return;
      }
      
      if (_presentationTimer.Enabled)
        _presentationTimer.Enabled = false;
      else {
        if (ACore.MediaItems.Current.MediaType == MediaType.Image && ACore.MediaItems.Current.IsPanoramatic)
          FullImage.Play(PresentationInterval, delegate { StartPresentationTimer(false); });
        else
          StartPresentationTimer(true);
      }
    }

    private static void CategoryGroupNew(object parameter) {
      (parameter as ViewModel.BaseCategoryItem)?.GroupNewOrRename(null, false);
    }

    private static void CategoryGroupRename(object parameter) {
      var group = parameter as Database.CategoryGroup;
      (group?.Parent as ViewModel.BaseCategoryItem)?.GroupNewOrRename(group, true);
    }

    private static void CategoryGroupDelete(object parameter) {
      var group = parameter as Database.CategoryGroup;
      (group?.Parent as ViewModel.BaseCategoryItem)?.GroupDelete(group);
    }

    private static void TagItemNew(object parameter) {
      var item = parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemNewOrRename(item, false);
    }

    private static void TagItemRename(object parameter) {
      var item = parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemNewOrRename(item, true);
    }

    private static void TagItemDelete(object parameter) {
      if (!MessageDialog.Show("Delete Confirmation", "Are you sure?", true)) return;
      var item = parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemDelete(item);
    }

    private static void ViewerIncludeFolder(object parameter) {
      ((Database.Viewer) parameter).AddFolder(true);
    }

    private static void ViewerExcludeFolder(object parameter) {
      ((Database.Viewer) parameter).AddFolder(false);
    }

    private static void ViewerRemoveFolder(object parameter) {
      Database.Viewers.RemoveFolder((ViewModel.BaseTreeViewItem) parameter);
    }

    private static void FolderNew(object parameter) {
      ((Database.Folder) parameter).NewOrRename(false);
    }

    private static void FolderRename(object parameter) {
      ((Database.Folder) parameter).NewOrRename(true);
    }

    private void FolderDelete(object parameter) {
      var folder = (Database.Folder) parameter;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you realy want to delete '{folder.Title}' folder?", true)) return;

      ACore.Folders.DeleteRecord(folder, true);
      // reload FolderKeywords
      ACore.FolderKeywords.Load();
    }

    private void FolderAddToFavorites(object parameter) {
      ACore.FavoriteFolders.Add((Database.Folder) parameter);
    }

    private void FolderRemoveFromFavorites(object parameter) {
      ACore.FavoriteFolders.Remove((Database.FavoriteFolder) parameter);
    }

    private void FolderSetAsFolderKeyword(object parameter) {
      ((Database.Folder) parameter).IsFolderKeyword = true;
      ACore.Folders.Helper.Table.SaveToFile();
      ACore.FolderKeywords.Load();
    }

    private void GeoNameNew(object parameter) {
      var inputDialog = new InputDialog {
        Owner = this,
        IconName = IconName.LocationCheckin,
        Title = "GeoName latitude and longitude",
        Question = "Enter in format: N36.75847,W3.84609",
        Answer = ""
      };

      inputDialog.BtnDialogOk.Click += delegate { inputDialog.DialogResult = true; };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        ((Database.GeoNames) parameter).New(inputDialog.Answer);
      }
    }

    private bool CanCompressPictures() {
      return ACore.MediaItems.Items.Count > 0;
    }

    private void CompressPictures() {
      var compress = new WCompress(ACore) {Owner = this};
      compress.ShowDialog();
    }

    private void OpenSettings() {
      var settings = new WSettings {Owner = this};
      if (settings.ShowDialog() ?? true) {
        Settings.Default.Save();
      }
      else {
        Settings.Default.Reload();
      }
    }

    private void OpenAbout() {
      var about = new WAbout {Owner = this};
      about.ShowDialog();
    }

    private void ShowHideTabMain(object parameter) {
      var show = false;
      var reload = false;
      if (parameter != null)
        show = (bool) parameter;
      else {
        switch (ACore.AppInfo.AppMode) {
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
        GridMain.ColumnDefinitions[0].Width = new GridLength(FlyoutMainTreeView.ActualWidth);
        GridMain.ColumnDefinitions[1].Width = new GridLength(3);
      }
      else {
        GridMain.ColumnDefinitions[0].Width = new GridLength(0);
        GridMain.ColumnDefinitions[1].Width = new GridLength(0);
      }

      FlyoutMainTreeView.IsPinned = show;
      FlyoutMainTreeView.IsOpen = show;

      if (show) {
        FlyoutStatusPanel.IsOpen = false;
        FlyoutStatusPanel.IsOpen = true;
      }

      if (reload) {
        ACore.MediaItems.SplitedItemsReload();
        ACore.MediaItems.ScrollTo(ACore.MediaItems.Current?.Index ?? 0);
      }
    }

    private void OpenFolderKeywordsList() {
      var fkl = new FolderKeywordList {Owner = this};
      fkl.ShowDialog();
    }

    private bool CanKeywordsEdit() {
      return !ACore.MediaItems.IsEditModeOn && ACore.MediaItems.Items.Count > 0;
    }

    private void KeywordsEdit() {
      Application.Current.Properties[nameof(AppProperty.EditKeywordsFromFolders)] = TabFolders.IsSelected;
      if (ACore.LastSelectedSource != null)
        ACore.LastSelectedSource.IsSelected = TabFolders.IsSelected;
      TabKeywords.IsSelected = true;
      ACore.MediaItems.IsEditModeOn = true;
    }

    private bool CanKeywordsSave() {
      return ACore.MediaItems.IsEditModeOn && ACore.MediaItems.Items.Count(p => p.IsModifed) > 0;
    }

    private void KeywordsSave() {
      var items = ACore.MediaItems.Items.Where(p => p.IsModifed).ToList();

      ACore.AppInfo.ProgressBarIsIndeterminate = false;
      ACore.AppInfo.ProgressBarValue = 0;

      using (var bw = new BackgroundWorker {WorkerReportsProgress = true}) {
        bw.ProgressChanged += delegate(object bwsender, ProgressChangedEventArgs bwe) {
          ACore.AppInfo.ProgressBarValue = bwe.ProgressPercentage;
        };

        bw.DoWork += delegate(object bwsender, DoWorkEventArgs bwe) {
          var worker = (BackgroundWorker) bwsender;
          var count = items.Count;
          var done = 0;
          bwe.Result = bwe.Argument;

          foreach (var item in items) {
            item.TryWriteMetadata();
            done++;
            worker.ReportProgress(Convert.ToInt32(((double) done / count) * 100), item.Index);
          }
        };

        bw.RunWorkerCompleted += delegate {
          ACore.MediaItems.IsEditModeOn = false;
          if ((bool) Application.Current.Properties[nameof(AppProperty.EditKeywordsFromFolders)]) {
            TabFolders.IsSelected = true;
          }

          foreach (var mi in ACore.MediaItems.Items.Where(mi => mi.IsModifed)) {
            mi.IsModifed = false;
          }

          ACore.MediaItems.IsEditModeOn = false;
          ACore.UpdateStatusBarInfo();
        };

        bw.RunWorkerAsync();
      }
    }

    private bool CanKeywordsCancel() {
      return ACore.MediaItems.IsEditModeOn;
    }

    private void KeywordsCancel() {
      foreach (var mi in ACore.MediaItems.Items.Where(x => x.IsModifed)) {
        mi.ReadMetadata();
        mi.IsModifed = false;
        mi.SetInfoBox();
      }

      ACore.Sdb.SaveAllTables();
      ACore.MarkUsedKeywordsAndPeople();
      ACore.MediaItems.IsEditModeOn = false;
      if ((bool) Application.Current.Properties[nameof(AppProperty.EditKeywordsFromFolders)]) {
        TabFolders.IsSelected = true;
      }
    }

    private bool CanKeywordsComment() {
      return ACore.MediaItems.Items.Count(x => x.IsSelected) == 1;
    }

    private void KeywordsComment() {
      var current = ACore.MediaItems.Current;
      var inputDialog = new InputDialog {
        Owner = this,
        IconName = IconName.Notification,
        Title = "Comment",
        Question = "Add a comment.",
        Answer = current.Comment
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (inputDialog.TxtAnswer.Text.Length > 256) {
          inputDialog.ShowErrorMessage("Comment is too long!");
          return;
        }

        if (AppCore.IncorrectChars.Any(inputDialog.TxtAnswer.Text.Contains)) {
          inputDialog.ShowErrorMessage("Comment contains incorrect character(s)!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      current.Comment = inputDialog.TxtAnswer.Text.Equals(string.Empty) ? null : inputDialog.TxtAnswer.Text;
      current.TryWriteMetadata();
      current.SetInfoBox();
      ACore.UpdateStatusBarInfo();
    }

    private void ReloadMetadata(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var progress = new ProgressBarDialog {Owner = this};

      progress.Worker.RunWorkerCompleted += delegate {
        progress.Close();
        ACore.Sdb.SaveAllTables();
      };

      progress.Worker.DoWork += delegate (object o, DoWorkEventArgs e) {
        var folder = parameter as Database.Folder;
        var mediaItems = folder != null
          ? folder.GetMediaItems(recursive)
          : ACore.MediaItems.GetSelectedOrAll();
        var worker = (BackgroundWorker) o;
        var count = mediaItems.Count;
        var done = 0;

        foreach (var mi in mediaItems.ToArray()) {
          if (worker.CancellationPending) {
            e.Cancel = true;
            break;
          }

          done++;
          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100),
            $"Processing file {done} of {count} ({mi.FileName})");

          mi.ReadMetadata();

          // set info box just for loaded media items
          if (folder == null)
            Application.Current.Dispatcher.Invoke(delegate { mi.SetInfoBox(); });
        }
      };

      progress.Worker.RunWorkerAsync();
      progress.Show();
    }

    private void RebuildThumbnails(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var progress = new ProgressBarDialog {Owner = this};

      progress.Worker.RunWorkerCompleted += delegate { progress.Close(); };

      progress.Worker.DoWork += delegate (object o, DoWorkEventArgs e) {
        var mediaItems = parameter is Database.Folder folder
          ? folder.GetMediaItems(recursive)
          : ACore.MediaItems.GetSelectedOrAll();
        var worker = (BackgroundWorker)o;
        var count = mediaItems.Count;
        var done = 0;

        foreach (var mi in mediaItems.ToArray()) {
          if (worker.CancellationPending) {
            e.Cancel = true;
            break;
          }

          done++;
          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100),
            $"Processing file {done} of {count} ({mi.FileName})");

          AppCore.CreateThumbnail(mi.FilePath, mi.FilePathCache, mi.ThumbSize);
        }
      };

      progress.Worker.RunWorkerAsync();
      progress.Show();
    }

    private bool CanAddGeoNamesFromFiles() {
      return ACore.MediaItems.Items.Count(x => x.IsSelected) > 0;
    }

    private void AddGeoNamesFromFiles() {
      var progress = new ProgressBarDialog {Owner = this};

      progress.Worker.RunWorkerCompleted += delegate { progress.Close(); };

      progress.Worker.DoWork += delegate(object o, DoWorkEventArgs e) {
        var worker = (BackgroundWorker) o;
        var mis = ACore.MediaItems.Items.Where(x => x.IsSelected).ToList();
        var count = mis.Count;
        var done = 0;

        foreach (var mi in mis) {
          if (worker.CancellationPending) {
            e.Cancel = true;
            break;
          }

          done++;
          worker.ReportProgress(Convert.ToInt32(((double) done / count) * 100),
            $"Processing file {done} of {count} ({mi.FileName})");

          if (mi.Lat == null || mi.Lng == null) mi.ReadMetadata(true);
          if (mi.Lat == null || mi.Lng == null) continue;

          var lastGeoName = ACore.GeoNames.InsertGeoNameHierarchy((double) mi.Lat, (double) mi.Lng);
          if (lastGeoName == null) continue;

          mi.GeoName = lastGeoName;
          mi.TryWriteMetadata();
        }
      };

      progress.Worker.RunWorkerAsync();
      progress.ShowDialog();
      ACore.Sdb.SaveAllTables();
    }

    private void ViewerChange(object parameter) {
      if (ACore.CurrentViewer != null)
        ACore.CurrentViewer.IsDefault = false;

      var viewer = (Database.Viewer) parameter;
      viewer.IsDefault = true;
      ACore.Viewers.Helper.Table.SaveToFile();

      MenuViewers.Header = viewer.Title;
      ACore.CurrentViewer = viewer;
      ACore.Folders.AddDrives();
      ACore.FolderKeywords.Load();
    }

    private void SwitchToFullScreen() {
      if (ACore.MediaItems.Current == null) return;
      ACore.AppInfo.AppMode = AppMode.Viewer;
      ShowHideTabMain(_mainTreeViewIsPinnedInViewer);
      ACore.UpdateStatusBarInfo();
      UseNoneWindowStyle = true;
      IgnoreTaskbarOnMaximize = true;
      MainMenu.Visibility = Visibility.Hidden;
    }

    private void SwitchToBrowser() {
      _presentationTimer.Enabled = false;
      ACore.AppInfo.AppMode = AppMode.Browser;
      ShowHideTabMain(_mainTreeViewIsPinnedInBrowser);
      ACore.MediaItems.SplitedItemsReload();
      ACore.MediaItems.ScrollToCurrent();
      ACore.MarkUsedKeywordsAndPeople();
      ACore.UpdateStatusBarInfo();
      UseNoneWindowStyle = false;
      ShowTitleBar = true;
      IgnoreTaskbarOnMaximize = false;
      MainMenu.Visibility = Visibility.Visible;
      FullImage.SetSource(null);
      FullImage.Stop();
      FullMedia.Source = null;
    }
  }
}