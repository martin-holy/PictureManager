using System;
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
          using (StreamReader reader = new StreamReader(stream)) {
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
      ShowFullPicture();
    }

    private void WbThumbs_MouseDown(object sender, HtmlElementEventArgs e) {
      if (e.MouseButtonsPressed == System.Windows.Forms.MouseButtons.Left) {
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
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      InitUi();
      //app opened with argument
      if (File.Exists(_argPicFile)) {
        ACore.AppInfo.AppMode = AppModes.Viewer;
        ACore.ViewerOnly = true;
        ACore.OneFileOnly = true;
        ACore.MediaItems.Items.Add(new ViewModel.Picture(_argPicFile, 0, null));
        ACore.MediaItems.Items[0].IsSelected = true;
        ACore.MediaItems.Current = ACore.MediaItems.Items[0];
        ShowFullPicture();
      } else {
        ACore.AppInfo.AppMode = AppModes.Browser;
        //InitUi();
      }
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

    public void ShowFullPicture() {
      if (ACore.MediaItems.Current == null) return;
      _wFullPic.SetCurrentImage();
      if (!_wFullPic.IsActive) {
        Hide();
        ShowInTaskbar = false;
        _wFullPic.Show();
      }
    }

    private void Window_Closing(object sender, CancelEventArgs e) {
      _wFullPic.Close();
    }

    public void SwitchToBrowser() {
      if (ACore.ViewerOnly) {
        ACore.ViewerOnly = false;
        var dirPath = Path.GetDirectoryName(_argPicFile);
        ACore.Folders.ExpandTo(dirPath);
        ACore.MediaItems.LoadByFolder(dirPath);
        ACore.InitThumbsPagesControl();
        ACore.MediaItems.Current = ACore.MediaItems.Items.SingleOrDefault(x => x.FilePath.Equals(_argPicFile));
      }
      if (ACore.MediaItems.Current != null) {
        CmbThumbPage.SelectedIndex = ACore.MediaItems.Current.Index / ACore.ThumbsPerPage;
      }
      ACore.MediaItems.ScrollToCurrent();
      ACore.MarkUsedKeywordsAndPeople();
      ACore.UpdateStatusBarInfo();
      ShowInTaskbar = true;
      Show();
    }

    private void TvKeywords_Select(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseUp on StackPanel in TreeView
      StackPanel stackPanel = (StackPanel)sender;

      if (e.ChangedButton != MouseButton.Right) {
        ACore.TreeView_KeywordsStackPanel_PreviewMouseUp(stackPanel.DataContext, e.ChangedButton, false);
      }
    }

    private void TvFolders_Select(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseUp on StackPanel in TreeView
      StackPanel stackPanel = (StackPanel)sender;
      object item = stackPanel.DataContext;

      if (e.ChangedButton == MouseButton.Left) {
        switch (item.GetType().Name) {
          case nameof(ViewModel.Folders):
          case nameof(ViewModel.FavoriteFolders): {
            ((ViewModel.BaseTreeViewItem) item).IsSelected = false;
            break;
          }
          case nameof(ViewModel.Folder): {
            var folder = (ViewModel.Folder) item;
            if (!folder.IsAccessible) {
              folder.IsSelected = false;
              return;
            }

            folder.IsSelected = true;
            ACore.LastSelectedSource = folder;

            if (ACore.ThumbsWebWorker != null && ACore.ThumbsWebWorker.IsBusy) {
              ACore.ThumbsWebWorker.CancelAsync();
              ACore.ThumbsResetEvent.WaitOne();
            }

            ACore.MediaItems.LoadByFolder(folder.FullPath);
            ACore.InitThumbsPagesControl();
            break;
          }
          case nameof(ViewModel.FavoriteFolder): {
            var folder = ACore.Folders.ExpandTo(((ViewModel.FavoriteFolder) item).FullPath);
            if (folder != null) {
              var visibleTreeIndex = 0;
              ACore.Folders.GetVisibleTreeIndexFor(ACore.Folders.Items, folder, ref visibleTreeIndex);
              var offset = (ACore.FavoriteFolders.Items.Count + 1 + visibleTreeIndex) * 25;
              TvFoldersScrollViewer.ScrollToVerticalOffset(offset);
            }
            break;
          }
        }
      }
    }

    #region Commands
    private void CmdKeywordShowAll(object sender, ExecutedRoutedEventArgs e) {
      ACore.TreeView_KeywordsStackPanel_PreviewMouseUp(e.Parameter, MouseButton.Left, true);
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
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result != MessageBoxResult.Yes) return;
      var item = e.Parameter as ViewModel.BaseTreeViewItem;
      (item?.GetTopParent() as ViewModel.BaseCategoryItem)?.ItemDelete(item as ViewModel.BaseTreeViewTagItem);
    }

    private void CmdFilterNew(object sender, ExecutedRoutedEventArgs e) {
      var parent = e.Parameter as ViewModel.Filter;
      var newFilter = new ViewModel.Filter {Parent = parent, Title = "New filter"};
      newFilter.FilterData.Add(new FilterGroup {Operator = FilterGroupOps.And});
      var fb = new WFilterBuilder(newFilter) {Owner = this};
      if (fb.ShowDialog() ?? true) {
        newFilter.SaveFilter();
        if (parent != null) {
          parent.Items.Add(newFilter);
        } else {
          ACore.Filters.Items.Add(newFilter);
        }
      }
    }

    private void CmdFilterEdit(object sender, ExecutedRoutedEventArgs e) {
      var filter = (ViewModel.Filter) e.Parameter;
      var title = filter.Title;
      var fb = new WFilterBuilder(filter) {Owner = this};
      if (fb.ShowDialog() ?? true) {
        filter.SaveFilter();
      } else {
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
      ACore.Viewers.AddFolder(true, ((ViewModel.Folder) e.Parameter).FullPath);
    }

    private void CmdViewerExcludeFolder(object sender, ExecutedRoutedEventArgs e) {
      ACore.Viewers.AddFolder(false, ((ViewModel.Folder)e.Parameter).FullPath);
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

    private void CmdFolderKeywordShowAll(object sender, ExecutedRoutedEventArgs e) {
      ACore.TreeView_KeywordsStackPanel_PreviewMouseUp(e.Parameter, MouseButton.Left, true);
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
      } else {
        Settings.Default.Reload();
      }
    }

    private void CmdAbout_Executed(object sender, ExecutedRoutedEventArgs e) {
      var about = new WAbout { Owner = this };
      about.ShowDialog();
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

      BackgroundWorker bw = new BackgroundWorker { WorkerReportsProgress = true };

      bw.ProgressChanged += delegate (object bwsender, ProgressChangedEventArgs bwe) {
        StatusProgressBar.Value = bwe.ProgressPercentage;
      };

      bw.DoWork += delegate (object bwsender, DoWorkEventArgs bwe) {
        var worker = (BackgroundWorker)bwsender;
        var count = pictures.Count;
        var done = 0;

        foreach (var picture in pictures) {
          picture.SaveMediaItemInToDb(false, false);
          picture.TryWriteMetadata();
          done++;
          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100), picture.Index);
        }
      };

      bw.RunWorkerCompleted += delegate {
        ACore.KeywordsEditMode = false;
        if ((bool) Application.Current.Properties[nameof(AppProps.EditKeywordsFromFolders)]) {
          TabFolders.IsSelected = true;
        }
        ACore.Db.SubmitChanges();
        foreach (var mi in ACore.MediaItems.Items.Where(mi => mi.IsModifed)) {
          mi.IsModifed = false;
        }
        ACore.AppInfo.AppMode = AppModes.Browser;
      };

      bw.RunWorkerAsync();
    }

    private void CmdKeywordsCancel_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = TabKeywords.IsSelected && ACore.KeywordsEditMode;
    }

    private void CmdKeywordsCancel_Executed(object sender, ExecutedRoutedEventArgs e) {
      foreach (ViewModel.BaseMediaItem mi in ACore.MediaItems.Items.Where(x => x.IsModifed)) {
        mi.ReLoadFromDb();
        mi.WbUpdateInfo();
      }
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
      InputDialog inputDialog = new InputDialog {
        Owner = this,
        IconName = "appbar_notification",
        Title = "Comment",
        Question = "Add a comment.",
        Answer = current.Comment
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

      if (inputDialog.ShowDialog() ?? true) {
        current.Comment = inputDialog.TxtAnswer.Text;
        current.SaveMediaItemInToDb(false, false);
        current.TryWriteMetadata();
        ACore.Db.SubmitChanges();
      }
    }

    private void CmdReloadMetadata_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.Items.Count > 0;
    }

    private void CmdReloadMetadata_Executed(object sender, ExecutedRoutedEventArgs e) {
      var mediaItems = ACore.MediaItems.GetSelectedOrAll();
      foreach (var mi in mediaItems) {
        mi.SaveMediaItemInToDb(true, false);
        mi.WbUpdateInfo();
      }
      ACore.Db.SubmitChanges();
    }

    public bool RotateJpeg(string filePath, int quality, Rotation rotation) {
      var original = new FileInfo(filePath);
      if (!original.Exists) return false;
      var temp = new FileInfo(original.FullName.Replace(".", "_temp."));

      const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      try {
        using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
          JpegBitmapEncoder encoder = new JpegBitmapEncoder {QualityLevel = quality, Rotation = rotation};

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

    private ScrollViewer _tvFoldersScrollViewer;
    private ScrollViewer TvFoldersScrollViewer {
      get {
        if (_tvFoldersScrollViewer != null) return _tvFoldersScrollViewer;
        DependencyObject border = VisualTreeHelper.GetChild(TvFolders, 0);
        if (border != null) {
          _tvFoldersScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
        }

        return _tvFoldersScrollViewer;
      }
    }

    private ScrollViewer _tvKeywordsScrollViewer;
    private ScrollViewer TvKeywordsScrollViewer {
      get {
        if (_tvKeywordsScrollViewer != null) return _tvKeywordsScrollViewer;
        DependencyObject border = VisualTreeHelper.GetChild(TvKeywords, 0);
        if (border != null) {
          _tvKeywordsScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
        }

        return _tvKeywordsScrollViewer;
      }
    }

    private void CmdTestButton_Executed(object sender, ExecutedRoutedEventArgs e) {

      /*foreach (var item in ACore.MediaItems.Items) {
        var destFilePath = item.FilePath.Replace(@"D:\Pictures\01 Digital_Foto\-=Hotovo\2016\", @"K:\Fotky\Pongo\");
        Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
        File.Copy(item.FilePath, destFilePath);
      }
      MessageBox.Show("Done");*/

      //var dir = new DataModel.Directory {Path = "ds"};

      /*var db = new DataModel.PmDataContext("Data Source = data.db");
      db.Load();
      var dir = new DataModel.Directory {Id = db.GetNextIdFor("Directories"), Path = "aaa"};
      db.InsertOnSubmit(dir);
      db.SubmitChanges();

      dir.Path = "www";
      db.UpdateOnSubmit(dir);
      db.SubmitChanges();

      db.DeleteOnSubmit(dir);
      db.SubmitChanges();*/


      /*var file1 = ShellStuff.FileInformation.GetFileIdInfo(@"c:\20150831_114319_Martin.jpg");
      var file2 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\!test\20150831_114319_Martin.jpg");
      var file3 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\Temp\20150831_114319_Martin.jpg");
      //3659174697441353
      var filePath = @"d:\!test\20150831_114319_Martin.jpg";
      var fileInfo = new FileInfo(filePath);

      foreach (var file in Directory.EnumerateFiles(@"d:\Pictures\01 Digital_Foto\-=Hotovo\2016\2016_04_07+ - Penedo Furado\")) {
        var x = ShellStuff.FileInformation.GetFileIdInfo(file);
      }

      var xx = ShellStuff.FileInformation.GetFileIdInfo(filePath);*/

      //var dirs = ACore.Db.ListDirectories.Where(x => !Directory.Exists(x.Path)).Select(x => x.Path);

      //var people = ACore.Db.DataContext.GetTable<DataModel.Person>();

      //PmDbContext context = new PmDbContext();
      //var data = context.Directories.ToList();


      //var list = TestGetDirectories();

      /*var cont = new DataContext(ACore.Db.DbConn);
      var dirs = cont.GetTable<DataModel.Directory>().ToList();
      var mitems = cont.GetTable<DataModel.MediaItem>().ToList();
      var dd = mitems.Where(x => x.FileName.StartsWith("2015"));
      var mip = cont.GetTable<DataModel.MediaItemPerson>();

      var testt =
        from dir in dirs
        join mitem in mitems on dir.Id equals mitem.DirectoryId
        where dir.Path.StartsWith("P:")
        select new {FileName = mitem.FileName, DirPath = dir.Path};*/







      /*DirectoryListDialog dld = new DirectoryListDialog {
        Title = "Catalog Folders",
        DirList = {SettingsPropertyName = "CatalogFolders" }
      };
      dld.ShowDialog();*/


      //var path = @"d:\Download\New\!iya";
      //var count = ACore.MediaItems.SuportedExts.Sum(ext => Directory.EnumerateFiles(path, ext.Replace(".", "*."), SearchOption.AllDirectories).Count());

      //var count = ACore.MediaItems.SuportedExts.Sum(ext => Directory.GetFiles(path, ext.Replace(".", "*."), SearchOption.AllDirectories).Count());

      //JpegTest();

      //RotateJpeg(@"d:\!test\TestInTest\20160209_143609.jpg", 80, Rotation.Rotate90);

      /*//cause blue screen :-(
      var filePath = @"d:\!test\TestInTest\20160209_143609.jpg";
      if (File.Exists(filePath)) {
        Process.Start("rundll32.exe", "shell32.dll, OpenAs_RunDLL " + filePath);
      }*/

      //MessageBox.Show((GC.GetTotalMemory(true) / 1024 / 1024).ToString());

      /*WTestThumbnailGallery ttg = new WTestThumbnailGallery();
      ttg.Show();
      ttg.AddPhotosInFolder(ACore.Pictures);*/
    }

    #endregion

    public void WbThumbsShowContextMenu() {
      /*ContextMenu cm = FindResource("MnuFolder") as ContextMenu;
      if (cm == null) return;
      cm.PlacementTarget = WbThumbs;
      cm.IsOpen = true;*/
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
      Vector diff = _dragDropStartPosition - e.GetPosition(null);
      if (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) &&
          !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;
      var stackPanel = e.OriginalSource as StackPanel;
      if (stackPanel == null) return;
      DragDrop.DoDragDrop(stackPanel, _dragDropObject, DragDropEffects.Move);
    }

    private void TvKeywords_AllowDropCheck(object sender, DragEventArgs e) {
      var pos = e.GetPosition(TvKeywords);
      if (pos.Y < 25) {
        TvKeywordsScrollViewer.ScrollToVerticalOffset(TvKeywordsScrollViewer.VerticalOffset - 25);
      } else if (TvKeywords.ActualHeight - pos.Y < 25) {
        TvKeywordsScrollViewer.ScrollToVerticalOffset(TvKeywordsScrollViewer.VerticalOffset + 25);
      }

      var dest = ((StackPanel) sender).DataContext;
      if (e.Data.GetDataPresent(typeof (ViewModel.Keyword))) {

        if (((dest as ViewModel.BaseCategoryItem)?.Category ?? (dest as ViewModel.CategoryGroup)?.Category) == Categories.Keywords)
          return;

        var srcData = e.Data.GetData(typeof(ViewModel.Keyword)) as ViewModel.Keyword;
        var destData = dest as ViewModel.Keyword;
        if (destData?.Parent == srcData?.Parent) return;

        e.Effects = DragDropEffects.None;
        e.Handled = true;
      } else if (e.Data.GetDataPresent(typeof (ViewModel.Person))) {
        if (((dest as ViewModel.BaseCategoryItem)?.Category ?? (dest as ViewModel.CategoryGroup)?.Category) == Categories.People) {
          var srcData = (ViewModel.Person) e.Data.GetData(typeof (ViewModel.Person));
          if (srcData != null && srcData.Parent != (ViewModel.BaseTreeViewItem) dest) return;
        }
        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
    }

    private void TvKeywords_OnDrop(object sender, DragEventArgs e) {
      var panel = (StackPanel)sender;

      if (e.Data.GetDataPresent(typeof (ViewModel.Keyword))) {
        var srcData = (ViewModel.Keyword)e.Data.GetData(typeof(ViewModel.Keyword));
        var destData = (ViewModel.BaseTreeViewItem)panel.DataContext;
        var dropOnTop = e.GetPosition(panel).Y < panel.ActualHeight / 2;
        if (srcData == null || destData == null) return;
        ACore.Keywords.ItemMove(srcData, destData, dropOnTop);
      } else if (e.Data.GetDataPresent(typeof (ViewModel.Person))) {
        var srcData = (ViewModel.Person)e.Data.GetData(typeof(ViewModel.Person));
        if (srcData == null) return;
        var destData = panel.DataContext as ViewModel.BaseTreeViewItem;
        ACore.People.ItemMove(srcData, destData);
      }
    }

    private void TvFolders_OnMouseMove(object sender, MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return;
      Vector diff = _dragDropStartPosition - e.GetPosition(null);
      if (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) &&
          !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;
      var stackPanel = e.OriginalSource as StackPanel;
      if (stackPanel == null) return;
      DragDrop.DoDragDrop(stackPanel, _dragDropObject, DragDropEffects.All);
    }

    private void TvFolders_AllowDropCheck(object sender, DragEventArgs e) {
      var pos = e.GetPosition(TvFolders);
      if (pos.Y < 25) {
        TvFoldersScrollViewer.ScrollToVerticalOffset(TvFoldersScrollViewer.VerticalOffset - 25);
      } else if (TvFolders.ActualHeight - pos.Y < 25) {
        TvFoldersScrollViewer.ScrollToVerticalOffset(TvFoldersScrollViewer.VerticalOffset + 25);
      }

      var thumbs = e.Data.GetDataPresent(DataFormats.FileDrop); //thumbnails drop
      if (thumbs) {
        var dragged = (string[]) e.Data.GetData(DataFormats.FileDrop);
        var selected = ACore.MediaItems.Items.Where(x => x.IsSelected).Select(p => p.FilePath).OrderBy(p => p).ToArray();
        thumbs = selected.SequenceEqual(dragged);
      }
      var srcData = (ViewModel.Folder) e.Data.GetData(typeof (ViewModel.Folder));
      var destData = (ViewModel.Folder) ((StackPanel) sender).DataContext;
      if ((srcData == null && !thumbs) || destData == null || srcData == destData || !destData.IsAccessible) {
        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
    }

    private void TvFolders_OnDrop(object sender, DragEventArgs e) {
      var thumbs = e.Data.GetDataPresent(DataFormats.FileDrop); //thumbnails drop
      var srcData = (ViewModel.Folder) e.Data.GetData(typeof (ViewModel.Folder));
      var destData = (ViewModel.Folder) ((StackPanel) sender).DataContext;
      var from = thumbs ? null : srcData.FullPath;
      var itemName = thumbs ? null : srcData.FullPath.Substring(srcData.FullPath.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase) + 1);

      var flag = e.KeyStates == DragDropKeyStates.ControlKey ? 
        ACore.FileOperation(FileOperations.Copy, from, destData.FullPath, itemName) : 
        ACore.FileOperation(FileOperations.Move, from, destData.FullPath, itemName);
      if (!flag) return;

      if (thumbs) {
        if (e.KeyStates != DragDropKeyStates.ControlKey) {
          ACore.MediaItems.RemoveSelectedFromWeb();
          ACore.UpdateStatusBarInfo();
        }
        return;
      }

      if (e.KeyStates != DragDropKeyStates.ControlKey) {
        srcData.UpdateFullPath(((ViewModel.Folder) srcData.Parent).FullPath, destData.FullPath);
        srcData.Parent.Items.Remove(srcData);

        //check if was destination expanded
        if (destData.Items.Count == 1 && destData.Items[0].Title == @"...") return;

        srcData.Parent = destData;
        ViewModel.Folder folder = destData.Items.Cast<ViewModel.Folder>().FirstOrDefault(f => string.Compare(f.Title, srcData.Title, StringComparison.OrdinalIgnoreCase) >= 0);
        destData.Items.Insert(folder == null ? destData.Items.Count : destData.Items.IndexOf(folder), srcData);
      } else {
        destData.GetSubFolders(true);
      }
    }

    private void MenuAddItem(ItemsControl menu, string resourceName, object item) {
      menu.Items.Add(new MenuItem { Command = (ICommand)Resources[resourceName], CommandParameter = item });
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseRightButtonDown on StackPanel in TreeView
      e.Handled = true;
      StackPanel stackPanel = (StackPanel) sender;
      object item = stackPanel.DataContext;

      //if (stackPanel.ContextMenu != null) return;
      ContextMenu menu = new ContextMenu {Tag = item};

      var category = (item as ViewModel.BaseTreeViewItem)?.GetTopParent() as ViewModel.BaseCategoryItem;
      if (category != null) {
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


      switch (item.GetType().Name) {
        case nameof(ViewModel.Folder): {
          MenuAddItem(menu, "FolderNew", item);
          if (((ViewModel.Folder) item).Parent != null) {
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
        case nameof(ViewModel.FavoriteFolder): {
          MenuAddItem(menu, "FolderRemoveFromFavorites", item);
          break;
        }
        case nameof(ViewModel.FolderKeyword): {
          if (!ACore.KeywordsEditMode) {
            MenuAddItem(menu, "FolderKeywordShowAll", item);
          }
          break;
        }
        case nameof(ViewModel.Keyword): {
          if (!ACore.KeywordsEditMode) {
            MenuAddItem(menu, "KeywordShowAll", item);
          }
          break;
        }
        case nameof(ViewModel.Filters): {
          MenuAddItem(menu, "FilterNew", item);
          break;
        }
        case nameof(ViewModel.Filter): {
          MenuAddItem(menu, "FilterNew", item);
          MenuAddItem(menu, "FilterEdit", item);
          MenuAddItem(menu, "FilterDelete", item);
          break;
        }
        case nameof(ViewModel.Viewer): {
          MenuAddItem(menu, "ViewerEdit", item);
          break;
        }
        case nameof(ViewModel.BaseTreeViewItem): {
          if (((ViewModel.BaseTreeViewItem) item).Tag is DataModel.ViewerAccess)
            MenuAddItem(menu, "ViewerRemoveFolder", item);
          break;
        }
      }

      if (menu.Items.Count > 0)
        stackPanel.ContextMenu = menu;
    }

    private void CmbThumbPage_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (CmbThumbPage.SelectedIndex == -1) return;
      ACore.ThumbsPageIndex = CmbThumbPage.SelectedIndex;
      ACore.CreateThumbnailsWebPage();
    }

    private void CmbViewers_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
      var viewer = CmbViewers.SelectedItem as ViewModel.Viewer;
      if (viewer == null) return;
      ACore.CurrentViewer = viewer;
      Settings.Default.Viewer = viewer.Title;
      Settings.Default.Save();
      ACore.FolderKeywords.Load();
      ACore.Folders.AddDrives();
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
          ACore.Db.SubmitChanges();
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
          ACore.Db.RollbackChanges();
          ((ViewModel.Viewer) Application.Current.Properties[nameof(AppProps.EditedViewer)])?.ReLoad();
          ACore.AppInfo.AppMode = AppModes.Browser;
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }
  }
}
