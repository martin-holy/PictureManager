using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PictureManager.Dialogs;
using PictureManager.Properties;
using PictureManager.ViewModel;
using PictureManager.Database;

namespace PictureManager {
  public partial class WMain {

    private void AddCommandBindings() {
      // Window Commands
      CommandBindings.Add(new CommandBinding(Commands.SwitchToFullScreen, HandleExecute(SwitchToFullScreen), HandleCanExecute(CanSwitchToFullScreen)));
      CommandBindings.Add(new CommandBinding(Commands.SwitchToBrowser, HandleExecute(SwitchToBrowser), HandleCanExecute(CanSwitchToBrowser)));
      
      // MediaItems Commands
      CommandBindings.Add(new CommandBinding(Commands.MediaItemNext, HandleExecute(MediaItemNext), HandleCanExecute(CanMediaItemNext)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemPrevious, HandleExecute(MediaItemPrevious), HandleCanExecute(CanMediaItemPrevious)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsSelectAll, HandleExecute(MediaItemsSelectAll), HandleCanExecute(CanMediaItemsSelectAll)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsSelectNotModifed, HandleExecute(MediaItemsSelectNotModifed), HandleCanExecute(CanMediaItemsSelectNotModifed)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsDelete, HandleExecute(MediaItemsDelete), HandleCanExecute(CanMediaItemsDelete)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsLoadByTag, HandleExecute(MediaItemsLoadByTag)));
      CommandBindings.Add(new CommandBinding(Commands.Presentation, HandleExecute(Presentation), HandleCanExecute(CanPresentation)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsCompress, HandleExecute(MediaItemsCompress), HandleCanExecute(CanMediaItemsCompress)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsRotate, HandleExecute(MediaItemsRotate), HandleCanExecute(CanMediaItemsRotate)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsRebuildThumbnails, HandleExecute(MediaItemsRebuildThumbnails), HandleCanExecute(CanMediaItemsRebuildThumbnails)));
      CommandBindings.Add(new CommandBinding(Commands.MediaItemsShuffle, HandleExecute(MediaItemsShuffle), HandleCanExecute(CanMediaItemsShuffle)));
      
      // TreeView Commands
      CommandBindings.Add(new CommandBinding(Commands.CategoryGroupNew, HandleExecute(CategoryGroupNew)));
      CommandBindings.Add(new CommandBinding(Commands.CategoryGroupRename, HandleExecute(CategoryGroupRename)));
      CommandBindings.Add(new CommandBinding(Commands.CategoryGroupDelete, HandleExecute(CategoryGroupDelete)));
      CommandBindings.Add(new CommandBinding(Commands.TagItemNew, HandleExecute(TagItemNew)));
      CommandBindings.Add(new CommandBinding(Commands.TagItemRename, HandleExecute(TagItemRename)));
      CommandBindings.Add(new CommandBinding(Commands.TagItemDelete, HandleExecute(TagItemDelete)));
      CommandBindings.Add(new CommandBinding(Commands.TagItemDeleteNotUsed, HandleExecute(TagItemDeleteNotUsed)));
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
      
      // Metadata Commands
      CommandBindings.Add(new CommandBinding(Commands.MetadataEdit, HandleExecute(MetadataEdit), HandleCanExecute(CanMetadataEdit)));
      CommandBindings.Add(new CommandBinding(Commands.MetadataSave, HandleExecute(MetadataSave), HandleCanExecute(CanMetadataSave)));
      CommandBindings.Add(new CommandBinding(Commands.MetadataCancel, HandleExecute(MetadataCancel), HandleCanExecute(CanMetadataCancel)));
      CommandBindings.Add(new CommandBinding(Commands.MetadataComment, HandleExecute(MetadataComment), HandleCanExecute(CanMetadataComment)));
      CommandBindings.Add(new CommandBinding(Commands.MetadataReload, HandleExecute(MetadataReload), HandleCanExecute(CanMetadataReload)));
      CommandBindings.Add(new CommandBinding(Commands.MetadataReload2, HandleExecute(MetadataReload), HandleCanExecute(CanMetadataReload)));
      
      CommandBindings.Add(new CommandBinding(Commands.TestButton, HandleExecute(TestButton)));
      CommandBindings.Add(new CommandBinding(Commands.OpenSettings, HandleExecute(OpenSettings)));
      CommandBindings.Add(new CommandBinding(Commands.AddGeoNamesFromFiles, HandleExecute(AddGeoNamesFromFiles), HandleCanExecute(CanAddGeoNamesFromFiles)));
      CommandBindings.Add(new CommandBinding(Commands.ViewerChange, HandleExecute(ViewerChange)));
      CommandBindings.Add(new CommandBinding(Commands.OpenAbout, HandleExecute(OpenAbout)));
      CommandBindings.Add(new CommandBinding(Commands.OpenFolderKeywordsList, HandleExecute(OpenFolderKeywordsList)));
      CommandBindings.Add(new CommandBinding(Commands.ShowHideTabMain, HandleExecute(ShowHideTabMain)));
      CommandBindings.Add(new CommandBinding(Commands.OpenLog, HandleExecute(OpenLog)));
    }

    private void AddInputBindings() {
      MediaCommands.TogglePlayPause.InputGestures.Add(new KeyGesture(Key.Space));
      MediaCommands.TogglePlayPause.InputGestures.Add(new MouseGesture(MouseAction.LeftClick));

      SetTargetToCommand(MediaCommands.TogglePlayPause, FullMedia);
      SetTargetToCommand(Commands.MediaItemsSelectAll, ThumbsBox);
    }

    private void SetTargetToCommand(RoutedCommand command, IInputElement commandTarget) {
      foreach (InputGesture ig in command.InputGestures)
        InputBindings.Add(new InputBinding(command, ig) {CommandTarget = commandTarget});
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

    private static CanExecuteRoutedEventHandler HandleCanExecute(Func<object, bool> canExecute) {
      return (o, e) => {
        e.CanExecute = canExecute(e.Parameter);
        e.Handled = true;
      };
    }

    private static bool CanMediaItemNext() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.MediaItems.GetNext() != null;
    }

    private void MediaItemNext() {
      var current = App.Core.MediaItems.GetNext();
      App.Core.MediaItems.Current = current;
      SetMediaItemSource();
      if (_presentationTimer.Enabled && (current.MediaType == MediaType.Video || current.IsPanoramatic)) {
        _presentationTimer.Enabled = false;
        _presentationTimerPaused = true;

        if (current.MediaType == MediaType.Image && current.IsPanoramatic)
          FullImage.Play(PresentationInterval, delegate { StartPresentationTimer(false); });
      }

      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanMediaItemPrevious() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.MediaItems.GetPrevious() != null;
    }

    private void MediaItemPrevious() {
      App.Core.MediaItems.Current = App.Core.MediaItems.GetPrevious();
      SetMediaItemSource();
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanMediaItemsSelectAll() {
      return App.Core.AppInfo.AppMode == AppMode.Browser && App.Core.MediaItems.Items.Count > 0;
    }

    private static void MediaItemsSelectAll() {
      App.Core.MediaItems.SelectAll();
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanMediaItemsSelectNotModifed() {
      return App.Core.AppInfo.AppMode == AppMode.Browser && App.Core.MediaItems.Items.Count > 0;
    }

    private static void MediaItemsSelectNotModifed() {
      App.Core.MediaItems.Current = null;

      foreach (var mi in App.Core.MediaItems.Items) {
        App.Core.MediaItems.SetSelected(mi, false);
        if (!mi.IsModifed)
          App.Core.MediaItems.SetSelected(mi, true);
      }

      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static bool CanMediaItemsDelete() {
      return App.Core.MediaItems.Selected > 0;
    }

    private void MediaItemsDelete() {
      var count = App.Core.MediaItems.Items.Count(x => x.IsSelected);
      if (!MessageDialog.Show("Delete Confirmation", 
        $"Do you realy want to delete {count} item{(count > 1 ? "s" : string.Empty)}?", true)) return;

      App.Core.MediaItems.RemoveSelected(true);

      if (App.Core.AppInfo.AppMode == AppMode.Viewer) {
        if (App.Core.MediaItems.Current != null)
          SetMediaItemSource();
        else
          SwitchToBrowser();
      }
    }

    private static bool CanMediaItemsShuffle() {
      return App.Core.MediaItems.Items.Count > 0;
    }

    private static void MediaItemsShuffle() {
      App.Core.MediaItems.Items.Shuffle();
      App.Core.MediaItems.SplitedItemsReload();
    }

    private static void MediaItemsLoadByTag(object parameter) {
      App.Core.MediaItems.LoadByTag((BaseTreeViewTagItem) parameter, (Keyboard.Modifiers & ModifierKeys.Shift) > 0);
    }

    private static bool CanPresentation() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer && App.Core.MediaItems.Current != null;
    }

    private void Presentation() {
      if (FullImage.IsAnimationOn) {
        FullImage.Stop();
        return;
      }
      
      if (_presentationTimer.Enabled)
        _presentationTimer.Enabled = false;
      else {
        if (App.Core.MediaItems.Current.MediaType == MediaType.Image && App.Core.MediaItems.Current.IsPanoramatic)
          FullImage.Play(PresentationInterval, delegate { StartPresentationTimer(false); });
        else
          StartPresentationTimer(true);
      }
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
      if (!MessageDialog.Show("Delete Confirmation", $"Do you realy want to delete '{item.Title}'?", true)) return;
      (item.GetTopParent() as BaseCategoryItem)?.ItemDelete(item);
    }

    private static void TagItemDeleteNotUsed(object parameter) {
      if (!(parameter is BaseTreeViewItem item)) return;
      if (!(item.GetTopParent() is BaseCategoryItem topParent)) return;

      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you realy want to delete not used items in '{item.Title}'?", true)) return;

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
      ((Viewer) parameter).AddFolder(true);
    }

    private static void ViewerExcludeFolder(object parameter) {
      ((Viewer) parameter).AddFolder(false);
    }

    private static void ViewerRemoveFolder(object parameter) {
      Viewers.RemoveFolder((BaseTreeViewItem) parameter);
    }

    private static void FolderNew(object parameter) {
      ((Folder) parameter).NewOrRename(false);
    }

    private static void FolderRename(object parameter) {
      ((Folder) parameter).NewOrRename(true);
    }

    private static void FolderDelete(object parameter) {
      var folder = (Folder) parameter;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you realy want to delete '{folder.Title}' folder?", true)) return;

      App.Core.Folders.DeleteRecord(folder, true);
      // reload FolderKeywords
      App.Core.FolderKeywords.Load();
    }

    private static void FolderAddToFavorites(object parameter) {
      App.Core.FavoriteFolders.Add((Folder) parameter);
    }

    private static void FolderRemoveFromFavorites(object parameter) {
      App.Core.FavoriteFolders.Remove((FavoriteFolder) parameter);
    }

    private static void FolderSetAsFolderKeyword(object parameter) {
      ((Folder) parameter).IsFolderKeyword = true;
      App.Core.Folders.Helper.Table.SaveToFile();
      App.Core.FolderKeywords.Load();
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
        ((GeoNames) parameter).New(inputDialog.Answer);
      }
    }

    private static bool CanMediaItemsCompress() {
      return App.Core.MediaItems.Items.Count > 0;
    }

    private void MediaItemsCompress() {
      CompressDialog.ShowDialog(this);
    }

    private static bool CanMediaItemsRotate() {
      return App.Core.MediaItems.Items.Count(x => x.IsSelected) > 0;
    }

    private void MediaItemsRotate() {
      var rotation = RotationDialog.Show();
      if (rotation == Rotation.Rotate0) return;
      App.Core.MediaItems.SetOrientation(App.Core.MediaItems.Items.Where(x => x.IsSelected).ToList(), rotation);

      if (App.Core.AppInfo.AppMode != AppMode.Viewer) return;
      SetMediaItemSource();
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
        App.Core.MediaItems.SplitedItemsReload();
        App.Core.MediaItems.ScrollToCurrent();
      }
    }

    private void OpenFolderKeywordsList() {
      var fkl = new FolderKeywordList {Owner = this};
      fkl.ShowDialog();
    }

    private static bool CanMetadataEdit() {
      return !App.Core.MediaItems.IsEditModeOn && App.Core.MediaItems.Items.Count > 0;
    }

    private void MetadataEdit() {
      Application.Current.Properties[nameof(AppProperty.EditKeywordsFromFolders)] = TabFolders.IsSelected;
      if (App.Core.LastSelectedSource != null)
        App.Core.LastSelectedSource.IsSelected = TabFolders.IsSelected;
      TabKeywords.IsSelected = true;
      App.Core.MediaItems.IsEditModeOn = true;
    }

    private static bool CanMetadataSave() {
      return App.Core.MediaItems.IsEditModeOn && App.Core.MediaItems.ModifedItems.Count > 0;
    }

    private void MetadataSave() {
      var items = App.Core.MediaItems.ModifedItems.ToList();
      var progress = new ProgressBarDialog(this, true);

      progress.Worker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) {
        if (e.Cancelled) {
          progress.Close();
          MetadataCancel();
          return;
        }

        if ((bool)Application.Current.Properties[nameof(AppProperty.EditKeywordsFromFolders)])
          TabFolders.IsSelected = true;

        App.Core.MediaItems.IsEditModeOn = false;
        progress.Close();
      };

      progress.Worker.DoWork += delegate (object sender, DoWorkEventArgs e) {
        var worker = (BackgroundWorker)sender;
        var count = items.Count;
        var done = 0;

        foreach (var mi in items) {
          if (worker.CancellationPending) {
            e.Cancel = true;
            break;
          }

          mi.TryWriteMetadata();

          Application.Current.Dispatcher.Invoke(delegate {
            App.Core.MediaItems.SetModifed(mi, false);
          });
          
          done++;
          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100),
            $"Processing file {done} of {count} ({mi.FileName})");
        }
      };

      progress.Worker.RunWorkerAsync();
      progress.ShowDialog();
    }

    private static bool CanMetadataCancel() {
      return App.Core.MediaItems.IsEditModeOn;
    }

    private void MetadataCancel() {
      var items = App.Core.MediaItems.ModifedItems.ToList();
      var progress = new ProgressBarDialog(this, true);

      progress.Worker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) {
        if (e.Cancelled) {
          progress.Close();
          return;
        }

        App.Core.Sdb.SaveAllTables();
        App.Core.MarkUsedKeywordsAndPeople();
        App.Core.MediaItems.IsEditModeOn = false;
        if ((bool)Application.Current.Properties[nameof(AppProperty.EditKeywordsFromFolders)])
          TabFolders.IsSelected = true;

        progress.Close();
      };

      progress.Worker.DoWork += delegate (object sender, DoWorkEventArgs e) {
        var worker = (BackgroundWorker)sender;
        var count = items.Count;
        var done = 0;

        foreach (var mi in items) {
          if (worker.CancellationPending) {
            e.Cancel = true;
            break;
          }

          mi.ReadMetadata();

          Application.Current.Dispatcher.Invoke(delegate {
            App.Core.MediaItems.SetModifed(mi, false);
            mi.SetInfoBox();
          });

          done++;
          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100),
            $"Processing file {done} of {count} ({mi.FileName})");
        }
      };

      progress.Worker.RunWorkerAsync();
      progress.ShowDialog();
    }

    private static bool CanMetadataComment() {
      return App.Core.MediaItems.Items.Count(x => x.IsSelected) == 1;
    }

    private void MetadataComment() {
      var current = App.Core.MediaItems.Current;
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

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      current.Comment = MediaItems.NormalizeComment(inputDialog.TxtAnswer.Text);
      current.TryWriteMetadata();
      current.SetInfoBox();
      current.OnPropertyChanged(nameof(current.Comment));
      App.Core.AppInfo.OnPropertyChanged(nameof(App.Core.AppInfo.IsCommentVisible));
    }

    private bool CanMetadataReload(object parameter) {
      return parameter is Folder || App.Core.MediaItems.Items.Count > 0;
    }

    private void MetadataReload(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var progress = new ProgressBarDialog(this, true);

      progress.Worker.RunWorkerCompleted += delegate {
        App.Core.Sdb.SaveAllTables();
        progress.Close();
      };

      progress.Worker.DoWork += delegate (object o, DoWorkEventArgs e) {
        var folder = parameter as Folder;
        var mediaItems = folder != null
          ? folder.GetMediaItems(recursive)
          : App.Core.MediaItems.GetSelectedOrAll();
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

    public bool CanMediaItemsRebuildThumbnails(object parameter) {
      return parameter is Folder || App.Core.MediaItems.Items.Count > 0;
    }

    public void MediaItemsRebuildThumbnails(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var progress = new ProgressBarDialog(this, true);
      App.Core.ThumbProcessCounter = 0;

      progress.Worker.RunWorkerCompleted += delegate {
        progress.Close();
        App.Core.MediaItems.SplitedItemsReload();
        App.Core.MediaItems.ScrollToCurrent();
      };

      progress.Worker.DoWork += delegate (object o, DoWorkEventArgs e) {
        List<MediaItem> mediaItems;
        switch (parameter) {
          case Folder folder: mediaItems = folder.GetMediaItems(recursive); break;
          case List<MediaItem> items: mediaItems = items; break;
          default: mediaItems = App.Core.MediaItems.GetSelectedOrAll(); break;
        }

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

          while (App.Core.ThumbProcessCounter > 10) Thread.Sleep(100);
          App.Core.CreateThumbnail(mi);
        }
      };

      progress.Worker.RunWorkerAsync();
      progress.Show();
    }

    private static bool CanAddGeoNamesFromFiles() {
      return App.Core.MediaItems.Items.Count(x => x.IsSelected) > 0;
    }

    private void AddGeoNamesFromFiles() {
      var progress = new ProgressBarDialog(this, true);

      progress.Worker.RunWorkerCompleted += delegate {
        App.Core.Sdb.SaveAllTables();
        progress.Close();
      };

      progress.Worker.DoWork += delegate(object o, DoWorkEventArgs e) {
        var worker = (BackgroundWorker) o;
        var mis = App.Core.MediaItems.Items.Where(x => x.IsSelected).ToList();
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

          var lastGeoName = App.Core.GeoNames.InsertGeoNameHierarchy((double) mi.Lat, (double) mi.Lng);
          if (lastGeoName == null) continue;

          mi.GeoName = lastGeoName;
          mi.TryWriteMetadata();
        }
      };

      progress.Worker.RunWorkerAsync();
      progress.ShowDialog();
    }

    private void ViewerChange(object parameter) {
      if (App.Core.CurrentViewer != null)
        App.Core.CurrentViewer.IsDefault = false;

      var viewer = (Viewer) parameter;
      viewer.IsDefault = true;
      App.Core.Viewers.Helper.Table.SaveToFile();

      MenuViewers.Header = viewer.Title;
      App.Core.CurrentViewer = viewer;
      App.Core.Folders.AddDrives();
      App.Core.FolderKeywords.Load();
    }

    private static bool CanSwitchToFullScreen() {
      return App.Core.AppInfo.AppMode == AppMode.Browser;
    }

    private void SwitchToFullScreen() {
      if (App.Core.MediaItems.Current == null) return;
      App.Core.AppInfo.AppMode = AppMode.Viewer;
      ShowHideTabMain(_mainTreeViewIsPinnedInViewer);
      UseNoneWindowStyle = true;
      IgnoreTaskbarOnMaximize = true;
      MainMenu.Visibility = Visibility.Hidden;
    }

    private static bool CanSwitchToBrowser() {
      return App.Core.AppInfo.AppMode == AppMode.Viewer;
    }

    private void SwitchToBrowser() {
      _presentationTimer.Enabled = false;
      App.Core.AppInfo.AppMode = AppMode.Browser;
      ShowHideTabMain(_mainTreeViewIsPinnedInBrowser);
      App.Core.MediaItems.SplitedItemsReload();
      App.Core.MediaItems.ScrollToCurrent();
      App.Core.MarkUsedKeywordsAndPeople();
      UseNoneWindowStyle = false;
      ShowTitleBar = true;
      IgnoreTaskbarOnMaximize = false;
      MainMenu.Visibility = Visibility.Visible;
      FullImage.SetSource(null);
      FullImage.Stop();
      FullMedia.Source = null;
    }

    private void OpenLog() {
      var log = new LogDialog {Owner = this};
      log.ShowDialog();
    }
  }
}