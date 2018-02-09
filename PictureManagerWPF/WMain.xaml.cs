using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PictureManager.Dialogs;
using PictureManager.Properties;
using HtmlElementEventArgs = System.Windows.Forms.HtmlElementEventArgs;
using InputDialog = PictureManager.Dialogs.InputDialog;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WMain.xaml
  /// </summary>
  public partial class WMain {
    readonly string _argPicFile;
    private readonly WFullPic _wFullPic;
    public AppCore ACore;
    private Point _dragDropStartPosition;
    private object _dragDropObject;

    public WMain(string picFile) {
      System.Windows.Forms.Application.EnableVisualStyles();
      InitializeComponent();
      var ver = Assembly.GetEntryAssembly().GetName().Version;
      Title = $"{Title} {ver.Major}.{ver.Minor}";

      ACore = new AppCore {WbThumbs = WbThumbs, WMain = this};
      Application.Current.Properties[nameof(AppProps.AppCore)] = ACore;
      ACore.InitBase();
      MainStatusBar.DataContext = ACore.AppInfo;

      WbThumbs.ObjectForScripting = new ScriptManager(ACore);
      WbThumbs.DocumentCompleted += WbThumbs_DocumentCompleted;

      Stream stream = null;
      try {
        stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PictureManager.html.Thumbs.html");
        if (stream != null)
          using (var reader = new StreamReader(stream)) {
            stream = null;
            WbThumbs.DocumentText = reader.ReadToEnd();
          }
      }
      finally {
        stream?.Dispose();
      }

      _wFullPic = new WFullPic();
      _argPicFile = picFile;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      InitUi();
      if (!File.Exists(_argPicFile)) {
        ACore.AppInfo.AppMode = AppModes.Browser;
        return;
      }

      //app opened with argument
      ACore.AppInfo.AppMode = AppModes.Viewer;
      ACore.MediaItems.Load(ACore.Folders.ExpandTo(Path.GetDirectoryName(_argPicFile)), false);
      ACore.MediaItems.Current = ACore.MediaItems.Items.SingleOrDefault(x => x.FilePath.Equals(_argPicFile));
      if (ACore.MediaItems.Current != null) ACore.MediaItems.Current.IsSelected = true;
      SwitchToFullScreen();
      ACore.InitThumbsPagesControl();
    }

    private void Window_Closing(object sender, CancelEventArgs e) {
      _wFullPic.Close();
    }

    public void InitUi() {
      ACore.Init();
      ACore.Folders.IsExpanded = true;
      TvFolders.ItemsSource = ACore.FoldersRoot;
      TvKeywords.ItemsSource = ACore.KeywordsRoot;
      TvFilters.ItemsSource = ACore.FiltersRoot;
      CmbViewers.ItemsSource = ACore.Viewers.Items;
      ACore.CurrentViewer = ACore.Viewers.Items.SingleOrDefault(x => x.Title == Settings.Default.Viewer) as ViewModel.Viewer;
      CmbViewers.SelectedItem = ACore.CurrentViewer;
    }

    public void SwitchToFullScreen() {
      if (ACore.MediaItems.Current == null) return;
      _wFullPic.SetCurrentMediaItem();
      if (_wFullPic.IsActive) return;
      Hide();
      ShowInTaskbar = false;
      _wFullPic.Show();
    }

    public void SwitchToBrowser() {
      if (ACore.MediaItems.Current != null) {
        CmbThumbPage.SelectedIndex = ACore.MediaItems.Current.Index / ACore.ThumbsPerPage;
      }
      ACore.MediaItems.ScrollToCurrent();
      ACore.MarkUsedKeywordsAndPeople();
      ACore.UpdateStatusBarInfo();
      ShowInTaskbar = true;
      Show();
    }

    private void CmbThumbPage_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (CmbThumbPage.SelectedIndex == -1) return;
      ACore.ThumbsPageIndex = CmbThumbPage.SelectedIndex;
      ACore.CreateThumbnailsWebPage();
    }

    private void CmbViewers_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (!(CmbViewers.SelectedItem is ViewModel.Viewer viewer)) return;
      ACore.CurrentViewer = viewer;
      Settings.Default.Viewer = viewer.Title;
      Settings.Default.Save();
      ACore.FolderKeywords.Load();
      ACore.Folders.AddDrives();
    }

    private void MenuAddItem(ItemsControl menu, string resourceName, object item) {
      menu.Items.Add(new MenuItem { Command = (ICommand)Resources[resourceName], CommandParameter = item });
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseRightButtonDown on StackPanel in TreeView
      e.Handled = true;
      var stackPanel = (StackPanel)sender;
      var item = stackPanel.DataContext;

      //if (stackPanel.ContextMenu != null) return;
      var menu = new ContextMenu { Tag = item };

      if ((item as ViewModel.BaseTreeViewItem)?.GetTopParent() is ViewModel.BaseCategoryItem category) {

        if (item is ViewModel.BaseCategoryItem && category.Category == Categories.GeoNames) {
          MenuAddItem(menu, "GeoNameNew", item);
        }

        if (category.CanModifyItems) {
          var cat = item as ViewModel.BaseCategoryItem;
          var group = item as ViewModel.CategoryGroup;

          if (cat != null || group != null || category.CanHaveSubItems) {
            MenuAddItem(menu, "TagItemNew", item);
          }

          if (item is ViewModel.BaseTreeViewTagItem && group == null) {
            MenuAddItem(menu, "TagItemRename", item);
            MenuAddItem(menu, "TagItemDelete", item);
          }

          if (category.CanHaveGroups && cat != null) {
            MenuAddItem(menu, "CategoryGroupNew", item);
          }

          if (group != null) {
            MenuAddItem(menu, "CategoryGroupRename", item);
            MenuAddItem(menu, "CategoryGroupDelete", item);
          }
        }
      }


      switch (item) {
        case ViewModel.Folder folder: {
          MenuAddItem(menu, "FolderNew", item);
          if (folder.Parent != null) {
            MenuAddItem(menu, "FolderRename", item);
            MenuAddItem(menu, "FolderDelete", item);
            MenuAddItem(menu, "FolderAddToFavorites", item);
          }
          if (ACore.AppInfo.AppMode == AppModes.ViewerEdit) {
            MenuAddItem(menu, "ViewerIncludeFolder", item);
            MenuAddItem(menu, "ViewerExcludeFolder", item);
          }
          break;
        }
        case ViewModel.FavoriteFolder _: {
          MenuAddItem(menu, "FolderRemoveFromFavorites", item);
          break;
        }
        case ViewModel.Filters _: {
          MenuAddItem(menu, "FilterNew", item);
          break;
        }
        case ViewModel.Filter _: {
          MenuAddItem(menu, "FilterNew", item);
          MenuAddItem(menu, "FilterEdit", item);
          MenuAddItem(menu, "FilterDelete", item);
          break;
        }
        case ViewModel.Viewer _: {
          MenuAddItem(menu, "ViewerEdit", item);
          break;
        }
        case ViewModel.BaseTreeViewItem btvi: {
          if (btvi.Tag is DataModel.ViewerAccess)
            MenuAddItem(menu, "ViewerRemoveFolder", item);
          break;
        }
      }

      if (menu.Items.Count > 0)
        stackPanel.ContextMenu = menu;
    }

    //this is PreviewMouseUp on StackPanel in TreeView Folders, Keywords and Filters
    private void TreeView_Select(object sender, MouseButtonEventArgs e) {
      /*
       SHIFT key => recursive
       (Folder, FolderKeyword) => MBL => show, MBL+ctrl => and, MBL+alt => hide
       (Person, Keyword, GeoName)(filters) => MBL => or, MBL+ctrl => and, MBL+alt => hide
       (Rating)(filter) => MBL => OR between ratings, AND in files
       */
      if (e.ChangedButton != MouseButton.Left) return;

      ACore.TreeView_Select(((StackPanel)sender).DataContext,
        Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl),
        Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt),
        Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
    }

    private void BtnStatBarOk_OnClick(object sender, RoutedEventArgs e) {
      switch (ACore.AppInfo.AppMode) {
        case AppModes.Browser:
          break;
        case AppModes.Viewer:
          break;
        case AppModes.KeywordsEdit: {
          CmdKeywordsSave_Executed(null, null);
          break;
        }
        case AppModes.ViewerEdit:
          var editedViewer = (ViewModel.Viewer)Application.Current.Properties[nameof(AppProps.EditedViewer)];
          ACore.Db.SubmitChanges(editedViewer?.Lists);
          ACore.AppInfo.AppMode = AppModes.Browser;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void BtnStatBarCancel_OnClick(object sender, RoutedEventArgs e) {
      switch (ACore.AppInfo.AppMode) {
        case AppModes.Browser:
          break;
        case AppModes.Viewer:
          break;
        case AppModes.KeywordsEdit: {
          CmdKeywordsCancel_Executed(null, null);
          break;
        }
        case AppModes.ViewerEdit:
          var editedViewer = (ViewModel.Viewer)Application.Current.Properties[nameof(AppProps.EditedViewer)];
          ACore.Db.RollbackChanges(editedViewer?.Lists);
          editedViewer?.ReLoad();
          ACore.AppInfo.AppMode = AppModes.Browser;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    #region WbThumbs
    private void WbThumbs_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e) {
      if (WbThumbs.Document?.Body == null) return;
      WbThumbs.Document.MouseDown += WbThumbs_MouseDown;
      WbThumbs.Document.Body.DoubleClick += WbThumbs_DblClick;
      WbThumbs.Document.Body.KeyDown += WbThumbs_KeyDown;
    }

    private void WbThumbs_KeyDown(object sender, HtmlElementEventArgs e) {
      if (e.KeyPressedCode == 46) {//Delete 
        var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
          if (ACore.FileOperation(FileOperations.Delete, !e.ShiftKeyPressed))
            ACore.MediaItems.RemoveSelectedFromWeb();
      }

      if (e.CtrlKeyPressed && e.KeyPressedCode == 65) {
        ACore.MediaItems.SelectAll();
        ACore.MarkUsedKeywordsAndPeople();
        ACore.UpdateStatusBarInfo();
        e.ReturnValue = false;
      }

      if (e.CtrlKeyPressed && e.KeyPressedCode == 75) {
        if (ACore.MediaItems.Items.Count(x => x.IsSelected) == 1)
          CmdKeywordsComment_Executed(null, null);
        e.ReturnValue = false;
      }
    }

    private void WbThumbs_DblClick(object sender, HtmlElementEventArgs e) {
      var thumb = WbThumbs.Document?.GetElementFromPoint(e.ClientMousePosition)?.Parent;
      if (thumb == null) return;
      if (thumb.Id == "content" || thumb.Id == null) return;
      ACore.MediaItems.DeselectAll();
      ACore.MediaItems.Current = ACore.MediaItems.Items[int.Parse(thumb.Id)];
      ACore.MediaItems.Current.IsSelected = true;
      SwitchToFullScreen();
    }

    private void WbThumbs_MouseDown(object sender, HtmlElementEventArgs e) {
      if (e.MouseButtonsPressed != System.Windows.Forms.MouseButtons.Left) return;
      var thumb = WbThumbs.Document?.GetElementFromPoint(e.ClientMousePosition)?.Parent;
      if (thumb == null) return;

      if (thumb.Id == "content") {
        ACore.MediaItems.DeselectAll();
        ACore.UpdateStatusBarInfo();
        ACore.MarkUsedKeywordsAndPeople();
        return;
      }

      if (!thumb.GetAttribute("className").Contains("thumbBox")) return;
      var mi = ACore.MediaItems.Items[int.Parse(thumb.Id)];

      if (e.CtrlKeyPressed) {
        mi.IsSelected = !mi.IsSelected;
        ACore.MediaItems.SetCurrent();
        ACore.UpdateStatusBarInfo();
        ACore.MarkUsedKeywordsAndPeople();
        return;
      }

      var current = ACore.MediaItems.Current;
      if (e.ShiftKeyPressed && current != null) {
        ACore.MediaItems.DeselectAll();
        var start = mi.Index > current.Index ? current.Index : mi.Index;
        var stop = mi.Index > current.Index ? mi.Index : current.Index;
        for (var i = start; i < stop + 1; i++) {
          ACore.MediaItems.Items[i].IsSelected = true;
        }
      }

      if (!e.CtrlKeyPressed && !e.ShiftKeyPressed && !mi.IsSelected) {
        ACore.MediaItems.DeselectAll();
        mi.IsSelected = true;
      }

      ACore.MediaItems.SetCurrent();
      ACore.UpdateStatusBarInfo();
      ACore.MarkUsedKeywordsAndPeople();
    }

    public void WbThumbsShowContextMenu() {
      /*ContextMenu cm = FindResource("MnuFolder") as ContextMenu;
      if (cm == null) return;
      cm.PlacementTarget = WbThumbs;
      cm.IsOpen = true;*/
    }
    #endregion

    #region Commands
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
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result != MessageBoxResult.Yes) return;
      var item = e.Parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemDelete(item);
    }

    private void CmdFilterNew(object sender, ExecutedRoutedEventArgs e) {
      var parent = e.Parameter as ViewModel.Filter;
      var newFilter = new ViewModel.Filter { Parent = parent, Title = "New filter" };
      newFilter.FilterData.Add(new FilterGroup { Operator = FilterGroupOps.And });
      var fb = new WFilterBuilder(newFilter) { Owner = this };
      if (!(fb.ShowDialog() ?? true)) return;
      newFilter.SaveFilter();
      if (parent != null) 
        parent.Items.Add(newFilter);
      else 
        ACore.Filters.Items.Add(newFilter);
    }

    private void CmdFilterEdit(object sender, ExecutedRoutedEventArgs e) {
      var filter = (ViewModel.Filter)e.Parameter;
      var title = filter.Title;
      var fb = new WFilterBuilder(filter) { Owner = this };
      if (fb.ShowDialog() ?? true) {
        filter.SaveFilter();
      }
      else {
        filter.Title = title;
        filter.ReloadData();
      }
    }

    private void CmdFilterDelete(object sender, ExecutedRoutedEventArgs e) {

    }

    private void CmdViewerEdit(object sender, ExecutedRoutedEventArgs e) {
      ACore.AppInfo.AppMode = AppModes.ViewerEdit;
      Application.Current.Properties[nameof(AppProps.EditedViewer)] = e.Parameter;
    }

    private void CmdViewerIncludeFolder(object sender, ExecutedRoutedEventArgs e) {
      ACore.Viewers.AddFolder(true, ((ViewModel.Folder)e.Parameter).FullPath);
    }

    private void CmdViewerExcludeFolder(object sender, ExecutedRoutedEventArgs e) {
      ACore.Viewers.AddFolder(false, ((ViewModel.Folder)e.Parameter).FullPath);
    }

    private void CmdViewerRemoveFolder(object sender, ExecutedRoutedEventArgs e) {
      ACore.Viewers.RemoveFolder((ViewModel.BaseTreeViewItem)e.Parameter);
    }

    private void CmdFolderNew(object sender, ExecutedRoutedEventArgs e) {
      ((ViewModel.Folder)e.Parameter).NewOrRename(false);
    }

    private void CmdFolderRename(object sender, ExecutedRoutedEventArgs e) {
      ((ViewModel.Folder)e.Parameter).NewOrRename(true);
    }

    private void CmdFolderDelete(object sender, ExecutedRoutedEventArgs e) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
        ((ViewModel.Folder)e.Parameter).Delete(true);
    }

    private void CmdFolderAddToFavorites(object sender, ExecutedRoutedEventArgs e) {
      ACore.FavoriteFolders.Add(((ViewModel.Folder)e.Parameter).FullPath);
      ACore.FavoriteFolders.Load();
    }

    private void CmdFolderRemoveFromFavorites(object sender, ExecutedRoutedEventArgs e) {
      ACore.FavoriteFolders.Remove(((ViewModel.FavoriteFolder)e.Parameter).FullPath);
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

      inputDialog.BtnDialogOk.Click += delegate {
        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        ((ViewModel.GeoNames)e.Parameter).New(inputDialog.Answer);
      }
    }

    private void CmdAlways_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = true;
    }

    private void CmdCompressPictures_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.Items.Count > 0;
    }

    private void CmdCompressPictures_Executed(object sender, ExecutedRoutedEventArgs e) {
      var compress = new WCompress(ACore) { Owner = this };
      compress.ShowDialog();
    }

    private void CmdOpenSettings_Executed(object sender, ExecutedRoutedEventArgs e) {
      var settings = new WSettings { Owner = this };
      if (settings.ShowDialog() ?? true) {
        Settings.Default.Save();
        ACore.FolderKeywords.Load();
      }
      else {
        Settings.Default.Reload();
      }
    }

    private void CmdAbout_Executed(object sender, ExecutedRoutedEventArgs e) {
      var about = new WAbout { Owner = this };
      about.ShowDialog();
    }

    private void CmdShowHideTabMain_Executed(object sender, ExecutedRoutedEventArgs e) {
      var col = GridMain.ColumnDefinitions[0];
      if (col.ActualWidth > 0) {
        col.Tag = col.ActualWidth;
        col.Width = new GridLength(0);
      }
      else {
        col.Width = new GridLength((double?)col.Tag ?? 350);
      }
    }

    private void CmdCatalog_Executed(object sender, ExecutedRoutedEventArgs e) {
      var catalog = new WCatalog(ACore) { Owner = this };
      catalog.Show();
    }

    private void CmdKeywordsEdit_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = !ACore.KeywordsEditMode && ACore.MediaItems.Items.Count > 0;
    }

    private void CmdKeywordsEdit_Executed(object sender, ExecutedRoutedEventArgs e) {
      Application.Current.Properties[nameof(AppProps.EditKeywordsFromFolders)] = TabFolders.IsSelected;
      ACore.LastSelectedSource.IsSelected = TabFolders.IsSelected;
      TabKeywords.IsSelected = true;
      ACore.KeywordsEditMode = true;
      ACore.AppInfo.AppMode = AppModes.KeywordsEdit;
    }

    private void CmdKeywordsSave_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = TabKeywords.IsSelected && ACore.KeywordsEditMode && ACore.MediaItems.Items.Count(p => p.IsModifed) > 0;
    }

    private void CmdKeywordsSave_Executed(object sender, ExecutedRoutedEventArgs e) {
      var pictures = ACore.MediaItems.Items.Where(p => p.IsModifed).ToList();

      StatusProgressBar.Value = 0;
      StatusProgressBar.Maximum = 100;

      var bw = new BackgroundWorker { WorkerReportsProgress = true };

      bw.ProgressChanged += delegate (object bwsender, ProgressChangedEventArgs bwe) {
        StatusProgressBar.Value = bwe.ProgressPercentage;
      };

      bw.DoWork += delegate (object bwsender, DoWorkEventArgs bwe) {
        var worker = (BackgroundWorker)bwsender;
        var count = pictures.Count;
        var done = 0;
        bwe.Result = bwe.Argument;

        foreach (var picture in pictures) {
          picture.SaveMediaItemInToDb(false, (List<DataModel.BaseTable>[])bwe.Argument);
          picture.TryWriteMetadata();
          done++;
          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100), picture.Index);
        }
      };

      bw.RunWorkerCompleted += delegate (object bwsender, RunWorkerCompletedEventArgs bwe) {
        ACore.KeywordsEditMode = false;
        if ((bool)Application.Current.Properties[nameof(AppProps.EditKeywordsFromFolders)]) {
          TabFolders.IsSelected = true;
        }
        ACore.Db.SubmitChanges((List<DataModel.BaseTable>[])bwe.Result);
        foreach (var mi in ACore.MediaItems.Items.Where(mi => mi.IsModifed)) {
          mi.IsModifed = false;
        }
        ACore.AppInfo.AppMode = AppModes.Browser;
      };

      bw.RunWorkerAsync(ACore.Db.GetInsertUpdateDeleteLists());
    }

    private void CmdKeywordsCancel_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = TabKeywords.IsSelected && ACore.KeywordsEditMode;
    }

    private void CmdKeywordsCancel_Executed(object sender, ExecutedRoutedEventArgs e) {
      ACore.MediaItems.ReLoad(ACore.MediaItems.Items.Where(x => x.IsModifed).ToList());
      ACore.MarkUsedKeywordsAndPeople();
      ACore.KeywordsEditMode = false;
      ACore.AppInfo.AppMode = AppModes.Browser;
      if ((bool)Application.Current.Properties[nameof(AppProps.EditKeywordsFromFolders)]) {
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
        mi.WbUpdateInfo();
      }
      ACore.Db.SubmitChanges(lists);
    }

    private void CmdGeoNames_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.Items.Count(x => x.IsSelected) > 0;
    }

    private void CmdGeoNames_Executed(object sender, ExecutedRoutedEventArgs e) {
      var progress = new ProgressBarDialog { Owner = this };

      progress.Worker.DoWork += delegate (object o, DoWorkEventArgs args) {
        var worker = (BackgroundWorker)o;
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
          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100),
            $"Processing file {done} of {count} ({mi.Data.FileName})");

          if (mi.Lat == null || mi.Lng == null) mi.ReadMetadata(true);
          if (mi.Lat == null || mi.Lng == null) continue;

          var lastGeoName = ACore.GeoNames.InsertGeoNameHierarchy((double)mi.Lat, (double)mi.Lng);
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
    #endregion

    #region TvFolders
    private ScrollViewer _tvFoldersScrollViewer;
    public ScrollViewer TvFoldersScrollViewer {
      get {
        if (_tvFoldersScrollViewer != null) return _tvFoldersScrollViewer;
        var border = VisualTreeHelper.GetChild(TvFolders, 0);
        _tvFoldersScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;

        return _tvFoldersScrollViewer;
      }
    }

    private void TvFolders_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      var stackPanel = sender as StackPanel;
      if (stackPanel == null) return;
      _dragDropObject = stackPanel.DataContext;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void TvFolders_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      _dragDropObject = null;
    }

    private void TvFolders_OnMouseMove(object sender, MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      if (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) &&
          !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;
      if (!(e.OriginalSource is StackPanel stackPanel) || _dragDropObject == null) return;
      DragDrop.DoDragDrop(stackPanel, _dragDropObject, DragDropEffects.All);
    }

    private void TvFolders_AllowDropCheck(object sender, DragEventArgs e) {
      var pos = e.GetPosition(TvFolders);
      if (pos.Y < 25) {
        TvFoldersScrollViewer.ScrollToVerticalOffset(TvFoldersScrollViewer.VerticalOffset - 25);
      }
      else if (TvFolders.ActualHeight - pos.Y < 25) {
        TvFoldersScrollViewer.ScrollToVerticalOffset(TvFoldersScrollViewer.VerticalOffset + 25);
      }

      var thumbs = e.Data.GetDataPresent(DataFormats.FileDrop); //thumbnails drop
      if (thumbs) {
        var dragged = ((string[])e.Data.GetData(DataFormats.FileDrop))?.OrderBy(x => x).ToArray();
        var selected = ACore.MediaItems.Items.Where(x => x.IsSelected).Select(p => p.FilePath).OrderBy(p => p).ToArray();
        if (dragged != null) thumbs = selected.SequenceEqual(dragged);
      }
      var srcData = (ViewModel.Folder)e.Data.GetData(typeof(ViewModel.Folder));
      var destData = (ViewModel.Folder)((StackPanel)sender).DataContext;
      if ((srcData == null && !thumbs) || destData == null || srcData == destData || !destData.IsAccessible) {
        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
    }

    private void TvFolders_OnDrop(object sender, DragEventArgs e) {
      var thumbs = e.Data.GetDataPresent(DataFormats.FileDrop); //thumbnails drop
      var srcData = (ViewModel.Folder)e.Data.GetData(typeof(ViewModel.Folder));
      var destData = (ViewModel.Folder)((StackPanel)sender).DataContext;
      var from = thumbs ? null : srcData?.FullPath;
      var itemName = thumbs ? null : srcData?.FullPath.Substring(srcData.FullPath.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase) + 1);

      var flag = e.KeyStates == DragDropKeyStates.ControlKey ?
        ACore.FileOperation(FileOperations.Copy, @from, destData.FullPath, itemName) :
        ACore.FileOperation(FileOperations.Move, @from, destData.FullPath, itemName);
      if (!flag) return;

      if (thumbs) {
        if (e.KeyStates != DragDropKeyStates.ControlKey) {
          ACore.MediaItems.RemoveSelectedFromWeb();
          ACore.UpdateStatusBarInfo();
        }
        return;
      }

      if (e.KeyStates != DragDropKeyStates.ControlKey) {
        if (srcData != null) {
          srcData.UpdateFullPath(((ViewModel.Folder)srcData.Parent).FullPath, destData.FullPath);
          srcData.Parent.Items.Remove(srcData);

          //check if was destination expanded
          if (destData.Items.Count == 1 && destData.Items[0].Title == @"...") return;

          srcData.Parent = destData;
          var folder = destData.Items.Cast<ViewModel.Folder>().FirstOrDefault(f => string.Compare(f.Title, srcData.Title, StringComparison.OrdinalIgnoreCase) >= 0);
          destData.Items.Insert(folder == null ? destData.Items.Count : destData.Items.IndexOf(folder), srcData);
        }
      }
      else {
        destData.GetSubFolders(true);
      }
    }
    #endregion

    #region TvKeywords
    private ScrollViewer _tvKeywordsScrollViewer;
    private ScrollViewer TvKeywordsScrollViewer {
      get {
        if (_tvKeywordsScrollViewer != null) return _tvKeywordsScrollViewer;
        var border = VisualTreeHelper.GetChild(TvKeywords, 0);
        _tvKeywordsScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;

        return _tvKeywordsScrollViewer;
      }
    }

    private void TvKeywords_OnMouseLeftButtonDown(object sender, MouseEventArgs e) {
      var stackPanel = sender as StackPanel;
      if (stackPanel == null) return;
      _dragDropObject = stackPanel.DataContext;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void TvKeywords_OnMouseLeftButtonUp(object sender, MouseEventArgs e) {
      _dragDropObject = null;
    }

    private void TvKeywords_OnMouseMove(object sender, MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      if (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) &&
          !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;
      var stackPanel = e.OriginalSource as StackPanel;
      if (stackPanel == null || _dragDropObject == null) return;
      DragDrop.DoDragDrop(stackPanel, _dragDropObject, DragDropEffects.Move);
    }

    private void TvKeywords_AllowDropCheck(object sender, DragEventArgs e) {
      var pos = e.GetPosition(TvKeywords);
      if (pos.Y < 25) {
        TvKeywordsScrollViewer.ScrollToVerticalOffset(TvKeywordsScrollViewer.VerticalOffset - 25);
      }
      else if (TvKeywords.ActualHeight - pos.Y < 25) {
        TvKeywordsScrollViewer.ScrollToVerticalOffset(TvKeywordsScrollViewer.VerticalOffset + 25);
      }

      var dest = ((StackPanel)sender).DataContext;
      if (e.Data.GetDataPresent(typeof(ViewModel.Keyword))) {

        if (((dest as ViewModel.BaseCategoryItem)?.Category ?? (dest as ViewModel.CategoryGroup)?.Category) == Categories.Keywords)
          return;

        var srcData = e.Data.GetData(typeof(ViewModel.Keyword)) as ViewModel.Keyword;
        var destData = dest as ViewModel.Keyword;
        if (destData?.Parent == srcData?.Parent) return;

        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
      else if (e.Data.GetDataPresent(typeof(ViewModel.Person))) {
        if (((dest as ViewModel.BaseCategoryItem)?.Category ?? (dest as ViewModel.CategoryGroup)?.Category) == Categories.People) {
          var srcData = (ViewModel.Person)e.Data.GetData(typeof(ViewModel.Person));
          if (srcData != null && srcData.Parent != (ViewModel.BaseTreeViewItem)dest) return;
        }
        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
    }

    private void TvKeywords_OnDrop(object sender, DragEventArgs e) {
      var panel = (StackPanel)sender;

      if (e.Data.GetDataPresent(typeof(ViewModel.Keyword))) {
        var srcData = (ViewModel.Keyword)e.Data.GetData(typeof(ViewModel.Keyword));
        var destData = (ViewModel.BaseTreeViewItem)panel.DataContext;
        var dropOnTop = e.GetPosition(panel).Y < panel.ActualHeight / 2;
        if (srcData == null || destData == null) return;
        ACore.Keywords.ItemMove(srcData, destData, dropOnTop);
      }
      else if (e.Data.GetDataPresent(typeof(ViewModel.Person))) {
        var srcData = (ViewModel.Person)e.Data.GetData(typeof(ViewModel.Person));
        if (srcData == null) return;
        var destData = panel.DataContext as ViewModel.BaseTreeViewItem;
        ACore.People.ItemMove(srcData, destData, srcData.Data.Id);
      }
    }
    #endregion

    public bool RotateJpeg(string filePath, int quality, Rotation rotation) {
      var original = new FileInfo(filePath);
      if (!original.Exists) return false;
      var temp = new FileInfo(original.FullName.Replace(".", "_temp."));

      const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      try {
        using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
          JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = quality, Rotation = rotation };

          //BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile and BitmapCacheOption.None
          //is a KEY to lossless jpeg edit if the QualityLevel is the same
          encoder.Frames.Add(BitmapFrame.Create(originalFileStream, createOptions, BitmapCacheOption.None));

          using (Stream newFileStream = File.Open(temp.FullName, FileMode.Create, FileAccess.ReadWrite)) {
            encoder.Save(newFileStream);
          }
        }
      }
      catch (Exception) {
        return false;
      }

      try {
        temp.CreationTime = original.CreationTime;
        original.Delete();
        temp.MoveTo(original.FullName);
      }
      catch (Exception) {
        return false;
      }

      return true;
    }

    /*private Dictionary<int, KeyValuePair<string, string>> GetFileProps(string filename) {
      var shl = new Shell32.Shell();
      var fldr = shl.NameSpace(Path.GetDirectoryName(filename));
      var itm = fldr.ParseName(Path.GetFileName(filename));
      var fileProps = new Dictionary<int, KeyValuePair<string, string>>();
      for (var i = 0; i < 1000; i++) {
        var propValue = fldr.GetDetailsOf(itm, i);
        if (propValue != "") {
          fileProps.Add(i, new KeyValuePair<string, string>(fldr.GetDetailsOf(null, i), propValue));
        }
      }
      return fileProps;
    }*/

    private void CmdTestButton_Executed(object sender, ExecutedRoutedEventArgs e) {
      //var folder = new ViewModel.Folder { FullPath = @"d:\Pictures\01 Digital_Foto\-=Hotovo\2016" };
      //var fk = ACore.FolderKeywords.GetFolderKeywordByFullPath(folder.FullPath);
      //ACore.MediaItems.Load(folder, true);
      //ACore.MediaItems.Load(fk, true);
      //ACore.MediaItems.LoadByTag(fk, true);
      //ACore.MediaItems.LoadByFolder(folder.FullPath, true);
      //ACore.InitThumbsPagesControl();

      ACore.MediaItems.LoadPeople(ACore.MediaItems.Items.ToList());


      //var file1 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\video.mp4");
      /*var x = GetFileProps(@"d:\video.mp4");
      var xx = ShellStuff.FileInformation.GetVideoDimensions(@"d:\video.mp4");
      var doc = WbThumbs.Document.All;
      AppCore.CreateThumbnail(@"d:\video.mp4", @"d:\video.jpg");*/

      //height 309, width 311

      /*var file1 = ShellStuff.FileInformation.GetFileIdInfo(@"c:\20150831_114319_Martin.jpg");
      var file2 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\!test\20150831_114319_Martin.jpg");
      var file3 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\Temp\20150831_114319_Martin.jpg");
      //3659174697441353
      var filePath = @"d:\!test\20150831_114319_Martin.jpg";
      var fileInfo = new FileInfo(filePath);*/
    }
  }
}
