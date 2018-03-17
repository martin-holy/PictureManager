using System;
using System.Collections.Generic;
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
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsDelete, HandleExecute(MediaItemsDelete), HandleCanExecute(CanMediaItemsDelete)));
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
      CommandBindings.Add(new CommandBinding(Commands.FilterNew, HandleExecute(FilterNew)));
      CommandBindings.Add(new CommandBinding(Commands.FilterEdit, HandleExecute(FilterEdit)));
      CommandBindings.Add(new CommandBinding(Commands.FilterDelete, HandleExecute(FilterDelete)));
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
      CommandBindings.Add(new CommandBinding(Commands.ReloadMetadata, HandleExecute(ReloadMetadata), HandleCanExecute(CanReloadMetadata)));
      CommandBindings.Add(new CommandBinding(Commands.OpenSettings, HandleExecute(OpenSettings)));
      CommandBindings.Add(new CommandBinding(Commands.AddGeoNamesFromFiles, HandleExecute(AddGeoNamesFromFiles), HandleCanExecute(CanAddGeoNamesFromFiles)));
      CommandBindings.Add(new CommandBinding(Commands.ViewerChange, HandleExecute(ViewerChange)));
      CommandBindings.Add(new CommandBinding(Commands.OpenAbout, HandleExecute(OpenAbout)));
      CommandBindings.Add(new CommandBinding(Commands.OpenCatalog, HandleExecute(OpenCatalog)));
      CommandBindings.Add(new CommandBinding(Commands.ShowHideTabMain, HandleExecute(ShowHideTabMain)));
    }

    private void AddInputBindings() {
      AddInputBinding(Commands.SwitchToBrowser, new KeyGesture(Key.Escape), PanelFullScreen);
      AddInputBinding(Commands.MediaItemNext, new KeyGesture(Key.Right), PanelFullScreen);
      AddInputBinding(Commands.MediaItemPrevious, new KeyGesture(Key.Left), PanelFullScreen);
      AddInputBinding(Commands.MediaItemsSelectAll, new KeyGesture(Key.A, ModifierKeys.Control), ThumbsBox);
      AddInputBinding(Commands.MediaItemsDelete, new KeyGesture(Key.Delete), PanelFullScreen);
      AddInputBinding(MediaCommands.TogglePlayPause, new KeyGesture(Key.Space), FullMedia);
      AddInputBinding(MediaCommands.TogglePlayPause, new MouseGesture(MouseAction.LeftClick), FullMedia);
      
      AddInputBinding(Commands.KeywordsEdit, new KeyGesture(Key.E, ModifierKeys.Control));
      AddInputBinding(Commands.KeywordsSave, new KeyGesture(Key.S, ModifierKeys.Control));
      AddInputBinding(Commands.KeywordsCancel, new KeyGesture(Key.Q, ModifierKeys.Control));
      AddInputBinding(Commands.KeywordsComment, new KeyGesture(Key.K, ModifierKeys.Control));
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
      ACore.MediaItems.CurrentItemMove(true);
      SetMediaItemSource();
      ACore.UpdateStatusBarInfo();
    }

    private bool CanMediaItemPrevious() {
      return ACore.AppInfo.AppMode == AppMode.Viewer && ACore.MediaItems.Current?.Index > 0;
    }

    private void MediaItemPrevious() {
      ACore.MediaItems.CurrentItemMove(false);
      SetMediaItemSource();
      ACore.UpdateStatusBarInfo();
    }

    private bool CanMediaItemsSelectAll() {
      return ACore.AppInfo.AppMode == AppMode.Browser && ACore.MediaItems.Items.Count > 0;
    }

    private void MediaItemsSelectAll() {
      ACore.MediaItems.SelectAll();
      ACore.UpdateStatusBarInfo();
    }

    private bool CanMediaItemsDelete() {
      return ACore.AppInfo.Selected > 0;
    }

    private void MediaItemsDelete() {
      if (MessageBox.Show("Are you sure?", "Delete Confirmation", 
        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
      ACore.FileOperation(FileOperationMode.Delete, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
      ACore.MediaItems.RemoveSelected();
      if (ACore.AppInfo.AppMode == AppMode.Viewer) {
        if (CanMediaItemNext())
          MediaItemNext();
        else {
          if (CanMediaItemPrevious())
            MediaItemPrevious();
          else SwitchToBrowser();
        }
      }
      ACore.UpdateStatusBarInfo();
    }

    private static void CategoryGroupNew(object parameter) {
      (parameter as ViewModel.BaseCategoryItem)?.GroupNewOrRename(null, false);
    }

    private static void CategoryGroupRename(object parameter) {
      var group = parameter as ViewModel.CategoryGroup;
      (group?.Parent as ViewModel.BaseCategoryItem)?.GroupNewOrRename(group, true);
    }

    private static void CategoryGroupDelete(object parameter) {
      var group = parameter as ViewModel.CategoryGroup;
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
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo,
        MessageBoxImage.Question);
      if (result != MessageBoxResult.Yes) return;
      var item = parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemDelete(item);
    }

    private void FilterNew(object parameter) {
      var parent = parameter as ViewModel.Filter;
      var newFilter = new ViewModel.Filter {Parent = parent, Title = "New filter"};
      newFilter.FilterData.Add(new FilterGroup {Operator = FilterGroupOps.And});
      var fb = new WFilterBuilder(newFilter) {Owner = this};
      if (!(fb.ShowDialog() ?? true)) return;
      newFilter.SaveFilter();
      if (parent != null)
        parent.Items.Add(newFilter);
      else
        ACore.Filters.Items.Add(newFilter);
    }

    private void FilterEdit(object parameter) {
      var filter = (ViewModel.Filter) parameter;
      var title = filter.Title;
      var fb = new WFilterBuilder(filter) {Owner = this};
      if (fb.ShowDialog() ?? true) {
        filter.SaveFilter();
      }
      else {
        filter.Title = title;
        filter.ReloadData();
      }
    }

    private static void FilterDelete(object parameter) { }

    private static void ViewerIncludeFolder(object parameter) {
      ((ViewModel.Viewer) parameter).AddFolder(true);
    }

    private static void ViewerExcludeFolder(object parameter) {
      ((ViewModel.Viewer) parameter).AddFolder(false);
    }

    private void ViewerRemoveFolder(object parameter) {
      ACore.Viewers.RemoveFolder((ViewModel.BaseTreeViewItem) parameter);
    }

    private static void FolderNew(object parameter) {
      ((ViewModel.Folder) parameter).NewOrRename(false);
    }

    private static void FolderRename(object parameter) {
      ((ViewModel.Folder) parameter).NewOrRename(true);
    }

    private static void FolderDelete(object parameter) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo,
        MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
        ((ViewModel.Folder) parameter).Delete(true);
    }

    private void FolderAddToFavorites(object parameter) {
      ViewModel.FavoriteFolders.Add(((ViewModel.Folder) parameter).FullPath);
      ACore.FavoriteFolders.Load();
    }

    private void FolderRemoveFromFavorites(object parameter) {
      ViewModel.FavoriteFolders.Remove(((ViewModel.FavoriteFolder) parameter).FullPath);
      ACore.FavoriteFolders.Load();
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
        ((ViewModel.GeoNames) parameter).New(inputDialog.Answer);
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
        ACore.FolderKeywords.Load();
      }
      else {
        Settings.Default.Reload();
      }
    }

    private void OpenAbout() {
      var about = new WAbout {Owner = this};
      about.ShowDialog();
    }

    private void ShowHideTabMain() {
      var col = GridMain.ColumnDefinitions[0];
      if (col.ActualWidth > 0) {
        col.Tag = col.ActualWidth;
        col.Width = new GridLength(0);
      }
      else {
        col.Width = new GridLength((double?) col.Tag ?? 350);
      }
    }

    private void OpenCatalog() {
      var catalog = new WCatalog {Owner = this};
      catalog.Show();
    }

    private bool CanKeywordsEdit() {
      return !ACore.MediaItems.IsEditModeOn && ACore.MediaItems.Items.Count > 0;
    }

    private void KeywordsEdit() {
      Application.Current.Properties[nameof(AppProperty.EditKeywordsFromFolders)] = TabFolders.IsSelected;
      ACore.LastSelectedSource.IsSelected = TabFolders.IsSelected;
      TabKeywords.IsSelected = true;
      ACore.MediaItems.IsEditModeOn = true;
    }

    private bool CanKeywordsSave() {
      return ACore.MediaItems.IsEditModeOn && ACore.MediaItems.Items.Count(p => p.IsModifed) > 0;
    }

    private void KeywordsSave() {
      var items = ACore.MediaItems.Items.Where(p => p.IsModifed).ToList();

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
            item.SaveMediaItemInToDb(false, (List<DataModel.BaseTable>[]) bwe.Argument);
            item.TryWriteMetadata();
            done++;
            worker.ReportProgress(Convert.ToInt32(((double) done / count) * 100), item.Index);
          }
        };

        bw.RunWorkerCompleted += delegate(object bwsender, RunWorkerCompletedEventArgs bwe) {
          ACore.MediaItems.IsEditModeOn = false;
          if ((bool) Application.Current.Properties[nameof(AppProperty.EditKeywordsFromFolders)]) {
            TabFolders.IsSelected = true;
          }

          ACore.Db.SubmitChanges((List<DataModel.BaseTable>[]) bwe.Result);
          foreach (var mi in ACore.MediaItems.Items.Where(mi => mi.IsModifed)) {
            mi.IsModifed = false;
          }

          ACore.MediaItems.IsEditModeOn = false;
          ACore.UpdateStatusBarInfo();
        };

        bw.RunWorkerAsync(DataModel.PmDataContext.GetInsertUpdateDeleteLists());
      }
    }

    private bool CanKeywordsCancel() {
      return ACore.MediaItems.IsEditModeOn;
    }

    private void KeywordsCancel() {
      ACore.MediaItems.ReLoad(ACore.MediaItems.Items.Where(x => x.IsModifed).ToList());
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
        Answer = current.Data.Comment
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
      var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();
      current.Data.Comment = inputDialog.TxtAnswer.Text;
      current.SaveMediaItemInToDb(false, lists);
      current.TryWriteMetadata();
      current.SetInfoBox();
      ACore.Db.SubmitChanges(lists);
      ACore.UpdateStatusBarInfo();
    }

    private bool CanReloadMetadata() {
      return ACore.MediaItems.Items.Count > 0;
    }

    private void ReloadMetadata() {
      var mediaItems = ACore.MediaItems.GetSelectedOrAll();
      var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();
      foreach (var mi in mediaItems) {
        mi.SaveMediaItemInToDb(true, lists);
        AppCore.CreateThumbnail(mi.FilePath, mi.FilePathCache, mi.ThumbSize);
        mi.SetInfoBox();
      }

      ACore.Db.SubmitChanges(lists);
    }

    private bool CanAddGeoNamesFromFiles() {
      return ACore.MediaItems.Items.Count(x => x.IsSelected) > 0;
    }

    private void AddGeoNamesFromFiles() {
      var progress = new ProgressBarDialog {Owner = this};

      progress.Worker.DoWork += delegate(object o, DoWorkEventArgs args) {
        var worker = (BackgroundWorker) o;
        var mis = ACore.MediaItems.Items.Where(x => x.IsSelected).ToList();
        var count = mis.Count;
        var done = 0;
        var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();

        foreach (var mi in mis) {
          if (worker.CancellationPending) {
            args.Cancel = true;
            break;
          }

          done++;
          worker.ReportProgress(Convert.ToInt32(((double) done / count) * 100),
            $"Processing file {done} of {count} ({mi.Data.FileName})");

          if (mi.Lat == null || mi.Lng == null) mi.ReadMetadata(true);
          if (mi.Lat == null || mi.Lng == null) continue;

          var lastGeoName = ACore.GeoNames.InsertGeoNameHierarchy((double) mi.Lat, (double) mi.Lng);
          if (lastGeoName == null) continue;

          mi.Data.GeoNameId = lastGeoName.GeoNameId;
          DataModel.PmDataContext.UpdateOnSubmit(mi.Data, lists);
          mi.TryWriteMetadata();
        }

        ACore.Db.SubmitChanges(lists);
      };

      progress.Worker.RunWorkerAsync();
      progress.ShowDialog();
      ACore.GeoNames.Load();
    }

    private void ViewerChange(object parameter) {
      var viewer = (ViewModel.Viewer) parameter;
      MenuViewers.Header = viewer.Title;
      ACore.CurrentViewer = viewer;
      Settings.Default.Viewer = viewer.Title;
      Settings.Default.Save();
      ACore.FolderKeywords.Load();
      ACore.Folders.AddDrives();
    }

    private void SwitchToFullScreen() {
      if (ACore.MediaItems.Current == null) return;
      ACore.AppInfo.AppMode = AppMode.Viewer;
      ACore.UpdateStatusBarInfo();
      UseNoneWindowStyle = true;
      IgnoreTaskbarOnMaximize = true;
      MainMenu.Visibility = Visibility.Hidden;
      Application.Current.Properties[AppProperty.MainTreeViewWidth] = GridMain.ColumnDefinitions[0].ActualWidth;
      GridMain.ColumnDefinitions[0].Width = new GridLength(0);
      GridMain.ColumnDefinitions[1].Width = new GridLength(0);
    }

    private void SwitchToBrowser() {
      ACore.AppInfo.AppMode = AppMode.Browser;
      ACore.MediaItems.ScrollToCurrent();
      ACore.MarkUsedKeywordsAndPeople();
      ACore.UpdateStatusBarInfo();
      UseNoneWindowStyle = false;
      ShowTitleBar = true;
      IgnoreTaskbarOnMaximize = false;
      MainMenu.Visibility = Visibility.Visible;
      GridMain.ColumnDefinitions[0].Width = new GridLength((double)Application.Current.Properties[AppProperty.MainTreeViewWidth]);
      GridMain.ColumnDefinitions[1].Width = new GridLength(3);
      FullImage.SetSource(null);
      FullMedia.Source = null;
    }
  }
}