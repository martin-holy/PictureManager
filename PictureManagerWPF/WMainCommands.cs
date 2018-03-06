using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PictureManager.Dialogs;
using PictureManager.Properties;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace PictureManager {
  public partial class WMain {
    public static readonly RoutedUICommand CmdMediaItemNext =
      new RoutedUICommand {Text = "Next", InputGestures = {new KeyGesture(Key.Right)}};

    public static readonly RoutedUICommand CmdMediaItemPrevious =
      new RoutedUICommand {Text = "Previous", InputGestures = {new KeyGesture(Key.Left)}};

    public static readonly RoutedUICommand CmdMediaItemsSelectAll =
      new RoutedUICommand { Text = "Select All", InputGestures = { new KeyGesture(Key.A, ModifierKeys.Control) } };

    private void CmdMediaItemNext_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.AppInfo.AppMode == AppModes.Viewer && ACore.MediaItems.Current?.Index + 1 < ACore.MediaItems.Items.Count;
    }

    private void CmdMediaItemNext_Executed(object sender, ExecutedRoutedEventArgs e) {
      ACore.MediaItems.CurrentItemMove(true);
      SetMediaItemSource();
      ACore.UpdateStatusBarInfo();
    }

    private void CmdMediaItemPrevious_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.AppInfo.AppMode == AppModes.Viewer && ACore.MediaItems.Current?.Index > 0;
    }

    private void CmdMediaItemPrevious_Executed(object sender, ExecutedRoutedEventArgs e) {
      ACore.MediaItems.CurrentItemMove(false);
      SetMediaItemSource();
      ACore.UpdateStatusBarInfo();
    }

    private void CmdMediaItemsSelectAll_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.AppInfo.AppMode == AppModes.Browser && ACore.MediaItems.Items.Count > 0;
    }

    private void CmdMediaItemsSelectAll_Executed(object sender, ExecutedRoutedEventArgs e) {
      ACore.MediaItems.SelectAll();
      ACore.UpdateStatusBarInfo();
    }

    private void CmdCategoryGroupNew(object sender, ExecutedRoutedEventArgs e) {
      (e.Parameter as ViewModel.BaseCategoryItem)?.GroupNewOrRename(null, false);
    }

    private void CmdCategoryGroupRename(object sender, ExecutedRoutedEventArgs e) {
      var group = e.Parameter as ViewModel.CategoryGroup;
      (group?.Parent as ViewModel.BaseCategoryItem)?.GroupNewOrRename(group, true);
    }

    private void CmdCategoryGroupDelete(object sender, ExecutedRoutedEventArgs e) {
      var group = e.Parameter as ViewModel.CategoryGroup;
      (group?.Parent as ViewModel.BaseCategoryItem)?.GroupDelete(group);
    }

    private void CmdTagItemNew(object sender, ExecutedRoutedEventArgs e) {
      var item = e.Parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemNewOrRename(item, false);
    }

    private void CmdTagItemRename(object sender, ExecutedRoutedEventArgs e) {
      var item = e.Parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemNewOrRename(item, true);
    }

    private void CmdTagItemDelete(object sender, ExecutedRoutedEventArgs e) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo,
        MessageBoxImage.Question);
      if (result != MessageBoxResult.Yes) return;
      var item = e.Parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemDelete(item);
    }

    private void CmdFilterNew(object sender, ExecutedRoutedEventArgs e) {
      var parent = e.Parameter as ViewModel.Filter;
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

    private void CmdFilterEdit(object sender, ExecutedRoutedEventArgs e) {
      var filter = (ViewModel.Filter) e.Parameter;
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

    private void CmdFilterDelete(object sender, ExecutedRoutedEventArgs e) { }

    private void CmdViewerIncludeFolder(object sender, ExecutedRoutedEventArgs e) {
      ((ViewModel.Viewer) e.Parameter).AddFolder(true);
    }

    private void CmdViewerExcludeFolder(object sender, ExecutedRoutedEventArgs e) {
      ((ViewModel.Viewer) e.Parameter).AddFolder(false);
    }

    private void CmdViewerRemoveFolder(object sender, ExecutedRoutedEventArgs e) {
      ACore.Viewers.RemoveFolder((ViewModel.BaseTreeViewItem) e.Parameter);
    }

    private void CmdFolderNew(object sender, ExecutedRoutedEventArgs e) {
      ((ViewModel.Folder) e.Parameter).NewOrRename(false);
    }

    private void CmdFolderRename(object sender, ExecutedRoutedEventArgs e) {
      ((ViewModel.Folder) e.Parameter).NewOrRename(true);
    }

    private void CmdFolderDelete(object sender, ExecutedRoutedEventArgs e) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo,
        MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
        ((ViewModel.Folder) e.Parameter).Delete(true);
    }

    private void CmdFolderAddToFavorites(object sender, ExecutedRoutedEventArgs e) {
      ACore.FavoriteFolders.Add(((ViewModel.Folder) e.Parameter).FullPath);
      ACore.FavoriteFolders.Load();
    }

    private void CmdFolderRemoveFromFavorites(object sender, ExecutedRoutedEventArgs e) {
      ACore.FavoriteFolders.Remove(((ViewModel.FavoriteFolder) e.Parameter).FullPath);
      ACore.FavoriteFolders.Load();
    }

    private void CmdGeoNameNew(object sender, ExecutedRoutedEventArgs e) {
      var inputDialog = new InputDialog {
        Owner = this,
        IconName = "appbar_location_checkin",
        Title = "GeoName latitude and longitude",
        Question = "Enter in format: N36.75847,W3.84609",
        Answer = ""
      };

      inputDialog.BtnDialogOk.Click += delegate { inputDialog.DialogResult = true; };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        ((ViewModel.GeoNames) e.Parameter).New(inputDialog.Answer);
      }
    }

    private void CmdAlways_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = true;
    }

    private void CmdCompressPictures_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.Items.Count > 0;
    }

    private void CmdCompressPictures_Executed(object sender, ExecutedRoutedEventArgs e) {
      var compress = new WCompress(ACore) {Owner = this};
      compress.ShowDialog();
    }

    private void CmdOpenSettings_Executed(object sender, ExecutedRoutedEventArgs e) {
      var settings = new WSettings {Owner = this};
      if (settings.ShowDialog() ?? true) {
        Settings.Default.Save();
        ACore.FolderKeywords.Load();
      }
      else {
        Settings.Default.Reload();
      }
    }

    private void CmdAbout_Executed(object sender, ExecutedRoutedEventArgs e) {
      var about = new WAbout {Owner = this};
      about.ShowDialog();
    }

    private void CmdShowHideTabMain_Executed(object sender, ExecutedRoutedEventArgs e) {
      var col = GridMain.ColumnDefinitions[0];
      if (col.ActualWidth > 0) {
        col.Tag = col.ActualWidth;
        col.Width = new GridLength(0);
      }
      else {
        col.Width = new GridLength((double?) col.Tag ?? 350);
      }
    }

    private void CmdCatalog_Executed(object sender, ExecutedRoutedEventArgs e) {
      var catalog = new WCatalog(ACore) {Owner = this};
      catalog.Show();
    }

    private void CmdKeywordsEdit_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = !ACore.MediaItems.IsEditModeOn && ACore.MediaItems.Items.Count > 0;
    }

    private void CmdKeywordsEdit_Executed(object sender, ExecutedRoutedEventArgs e) {
      Application.Current.Properties[nameof(AppProps.EditKeywordsFromFolders)] = TabFolders.IsSelected;
      ACore.LastSelectedSource.IsSelected = TabFolders.IsSelected;
      TabKeywords.IsSelected = true;
      ACore.MediaItems.IsEditModeOn = true;
    }

    private void CmdKeywordsSave_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.IsEditModeOn && ACore.MediaItems.Items.Count(p => p.IsModifed) > 0;
    }

    private void CmdKeywordsSave_Executed(object sender, ExecutedRoutedEventArgs e) {
      var items = ACore.MediaItems.Items.Where(p => p.IsModifed).ToList();

      ACore.AppInfo.ProgressBarValue = 0;

      var bw = new BackgroundWorker {WorkerReportsProgress = true};

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
        if ((bool) Application.Current.Properties[nameof(AppProps.EditKeywordsFromFolders)]) {
          TabFolders.IsSelected = true;
        }

        ACore.Db.SubmitChanges((List<DataModel.BaseTable>[]) bwe.Result);
        foreach (var mi in ACore.MediaItems.Items.Where(mi => mi.IsModifed)) {
          mi.IsModifed = false;
        }

        ACore.MediaItems.IsEditModeOn = false;
        ACore.UpdateStatusBarInfo();
      };

      bw.RunWorkerAsync(ACore.Db.GetInsertUpdateDeleteLists());
    }

    private void CmdKeywordsCancel_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.IsEditModeOn;
    }

    private void CmdKeywordsCancel_Executed(object sender, ExecutedRoutedEventArgs e) {
      ACore.MediaItems.ReLoad(ACore.MediaItems.Items.Where(x => x.IsModifed).ToList());
      ACore.MarkUsedKeywordsAndPeople();
      ACore.MediaItems.IsEditModeOn = false;
      if ((bool) Application.Current.Properties[nameof(AppProps.EditKeywordsFromFolders)]) {
        TabFolders.IsSelected = true;
      }
    }

    private void CmdKeywordsComment_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.Items.Count(x => x.IsSelected) == 1;
    }

    private void CmdKeywordsComment_Executed(object sender, ExecutedRoutedEventArgs e) {
      var current = ACore.MediaItems.Current;
      var inputDialog = new InputDialog {
        Owner = this,
        IconName = "appbar_notification",
        Title = "Comment",
        Question = "Add a comment.",
        Answer = current.Data.Comment
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (inputDialog.TxtAnswer.Text.Length > 256) {
          inputDialog.ShowErrorMessage("Comment is too long!");
          return;
        }

        if (ACore.IncorectChars.Any(inputDialog.TxtAnswer.Text.Contains)) {
          inputDialog.ShowErrorMessage("Comment contains incorrect character(s)!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      var lists = ACore.Db.GetInsertUpdateDeleteLists();
      current.Data.Comment = inputDialog.TxtAnswer.Text;
      current.SaveMediaItemInToDb(false, lists);
      current.TryWriteMetadata();
      ACore.Db.SubmitChanges(lists);
    }

    private void CmdReloadMetadata_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.Items.Count > 0;
    }

    private void CmdReloadMetadata_Executed(object sender, ExecutedRoutedEventArgs e) {
      var mediaItems = ACore.MediaItems.GetSelectedOrAll();
      var lists = ACore.Db.GetInsertUpdateDeleteLists();
      foreach (var mi in mediaItems) {
        mi.SaveMediaItemInToDb(true, lists);
        AppCore.CreateThumbnail(mi.FilePath, mi.FilePathCache);
        mi.SetInfoBox();
      }

      ACore.Db.SubmitChanges(lists);
    }

    private void CmdGeoNames_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.Items.Count(x => x.IsSelected) > 0;
    }

    private void CmdGeoNames_Executed(object sender, ExecutedRoutedEventArgs e) {
      var progress = new ProgressBarDialog {Owner = this};

      progress.Worker.DoWork += delegate(object o, DoWorkEventArgs args) {
        var worker = (BackgroundWorker) o;
        var mis = ACore.MediaItems.Items.Where(x => x.IsSelected).ToList();
        var count = mis.Count;
        var done = 0;
        var lists = ACore.Db.GetInsertUpdateDeleteLists();

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
          ACore.Db.UpdateOnSubmit(mi.Data, lists);
          mi.TryWriteMetadata();
        }

        ACore.Db.SubmitChanges(lists);
      };

      progress.Worker.RunWorkerAsync();
      progress.ShowDialog();
      ACore.GeoNames.Load();
    }
  }
}