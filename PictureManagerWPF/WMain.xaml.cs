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
      MainStatusBar.DataContext = ACore.AppInfo;

      WbThumbs.ObjectForScripting = new ScriptManager(this);
      WbThumbs.DocumentCompleted += WbThumbs_DocumentCompleted;

      using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PictureManager.html.Thumbs.html"))
        if (stream != null)
          using (StreamReader reader = new StreamReader(stream)) {
            WbThumbs.DocumentText = reader.ReadToEnd();
          }

      _wFullPic = new WFullPic(this);
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
          if (ACore.FileOperation(AppCore.FileOperations.Delete, !e.ShiftKeyPressed))
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
      //app opened with argument
      if (File.Exists(_argPicFile)) {
        ACore.ViewerOnly = true;
        ACore.OneFileOnly = true;
        ACore.MediaItems.Items.Add(new ViewModel.Picture(_argPicFile, ACore.Db, 0, WbThumbs, null));
        ACore.MediaItems.Items[0].IsSelected = true;
        ShowFullPicture();
      } else {
        InitUi();
      }
    }

    public void InitUi() {
      ACore.Init();
      ACore.Folders.IsExpanded = true;
      ACore.Keywords.IsExpanded = true;
      TvFolders.ItemsSource = ACore.FoldersRoot;
      TvKeywords.ItemsSource = ACore.KeywordsRoot;
      TvFilters.ItemsSource = ACore.FiltersRoot;
    }

    public void ShowFullPicture() {
      if (ACore.MediaItems.Current == null) return;
      _wFullPic.SetCurrentImage();
      if (!_wFullPic.IsActive)
        _wFullPic.Show();
    }

    private void Window_Closing(object sender, CancelEventArgs e) {
      _wFullPic.Close();
    }

    public void SwitchToBrowser() {
      if (ACore.ViewerOnly) {
        //App is first time loaded to main window
        ACore.ViewerOnly = false;
        InitUi();
        ACore.Folders.ExpandTo(Path.GetDirectoryName(_argPicFile));
      }
      if (ACore.MediaItems.Current != null) {
        CmbThumbPage.SelectedIndex = ACore.MediaItems.Current.Index / ACore.ThumbsPerPage;
      }
      ACore.MediaItems.ScrollToCurrent();
      ACore.MarkUsedKeywordsAndPeople();
      ACore.UpdateStatusBarInfo();
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
            ACore.LastSelectedSourceRecursive = false;

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

    private void CmdKeywordNew(object sender, ExecutedRoutedEventArgs e) {
      var keyword = e.Parameter as ViewModel.Keyword;
      var keywords = e.Parameter as ViewModel.Keywords;
      if (keyword == null && keywords == null) return;
      ACore.Keywords.NewOrRename(this, keywords != null ? keywords.Items : keyword.Items, keyword, false);
    }

    private void CmdKeywordRename(object sender, ExecutedRoutedEventArgs e) {
      var keyword = e.Parameter as ViewModel.Keyword;
      if (keyword == null) return;
      ACore.Keywords.NewOrRename(this, keyword.Items, keyword, true);
    }

    private void CmdKeywordDelete(object sender, ExecutedRoutedEventArgs e) {
      ACore.Keywords.DeleteKeyword((ViewModel.Keyword) e.Parameter);
    }

    private void CmdPersonNew(object sender, ExecutedRoutedEventArgs e) {
      ACore.People.NewOrRenamePerson(this, null, e.Parameter as ViewModel.PeopleGroup, false);
    }

    private void CmdPersonRename(object sender, ExecutedRoutedEventArgs e) {
      ACore.People.NewOrRenamePerson(this, (ViewModel.Person) e.Parameter, null, true);
    }

    private void CmdPersonDelete(object sender, ExecutedRoutedEventArgs e) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
        ACore.People.DeletePerson((ViewModel.Person) e.Parameter);
    }

    private void CmdPeopleGroupNew(object sender, ExecutedRoutedEventArgs e) {
      ACore.People.NewOrRenameGroup(this, null, false);
    }

    private void CmdPeopleGroupRename(object sender, ExecutedRoutedEventArgs e) {
      ACore.People.NewOrRenameGroup(this, (ViewModel.PeopleGroup) e.Parameter, true);
    }

    private void CmdPeopleGroupDelete(object sender, ExecutedRoutedEventArgs e) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
        ACore.People.DeletePeopleGroup((ViewModel.PeopleGroup) e.Parameter);
    }

    private void CmdFilterNew(object sender, ExecutedRoutedEventArgs e) {
      var parent = e.Parameter as ViewModel.Filter;
      var newFilter = new ViewModel.Filter {Parent = parent, Db = ACore.Db, Title = "New filter"};
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

    private void CmdFolderNew(object sender, ExecutedRoutedEventArgs e) {
      ((ViewModel.Folder) e.Parameter).NewOrRename(this, false);
    }

    private void CmdFolderRename(object sender, ExecutedRoutedEventArgs e) {
      ((ViewModel.Folder) e.Parameter).NewOrRename(this, true);
    }

    private void CmdFolderDelete(object sender, ExecutedRoutedEventArgs e) {
      var result = MessageBox.Show("Are you sure?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result == MessageBoxResult.Yes)
        ((ViewModel.Folder)e.Parameter).Delete(ACore, true);
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
      e.CanExecute = TabKeywords.IsSelected && !ACore.KeywordsEditMode && ACore.MediaItems.Items.Count > 0;
    }

    private void CmdKeywordsEdit_Executed(object sender, ExecutedRoutedEventArgs e) {
      ACore.KeywordsEditMode = true;
      ACore.LastSelectedSource.IsSelected = false;
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
        var aCore = (AppCore)bwe.Argument;
        var count = pictures.Count;
        var done = 0;

        foreach (var picture in pictures) {
          picture.SaveMediaItemInToDb(aCore, false, false);
          picture.WriteMetadata();
          done++;
          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100), picture.Index);
        }
      };

      bw.RunWorkerCompleted += delegate {
        ACore.KeywordsEditMode = false;
      };

      bw.RunWorkerAsync(ACore);
    }

    private void CmdKeywordsCancel_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = TabKeywords.IsSelected && ACore.KeywordsEditMode;
    }

    private void CmdKeywordsCancel_Executed(object sender, ExecutedRoutedEventArgs e) {
      ACore.KeywordsEditMode = false;
      foreach (ViewModel.BaseMediaItem mi in ACore.MediaItems.Items.Where(x => x.IsModifed)) {
        mi.LoadFromDb(ACore, mi.Data);
        mi.WbUpdateInfo();
      }
      ACore.MarkUsedKeywordsAndPeople();
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
        current.SaveMediaItemInToDb(ACore, false, false);
        current.WriteMetadata();
      }
    }

    private void CmdReloadMetadata_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
      e.CanExecute = ACore.MediaItems.Items.Count > 0;
    }

    private void CmdReloadMetadata_Executed(object sender, ExecutedRoutedEventArgs e) {
      var mediaItems = ACore.MediaItems.GetSelectedOrAll();
      foreach (var mi in mediaItems) {
        mi.SaveMediaItemInToDb(ACore, true, false);
        mi.WbUpdateInfo();
      }
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
    private ScrollViewer TvFoldersScrollViewer
    {
      get
      {
        if (_tvFoldersScrollViewer != null) return _tvFoldersScrollViewer;
        DependencyObject border = VisualTreeHelper.GetChild(TvFolders, 0);
        if (border != null) {
          _tvFoldersScrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
        }

        return _tvFoldersScrollViewer;
      }
    }

    private void CmdTestButton_Executed(object sender, ExecutedRoutedEventArgs e) {
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
      var dataContext = ((StackPanel) sender).DataContext;
      if (e.Data.GetDataPresent(typeof (ViewModel.Keyword))) {
        var srcData = (ViewModel.Keyword)e.Data.GetData(typeof(ViewModel.Keyword));
        var destData = (ViewModel.Keyword) dataContext;
        if (srcData != null && destData != null && srcData != destData && srcData.Parent == destData.Parent) return;
        e.Effects = DragDropEffects.None;
        e.Handled = true;
      } else if (e.Data.GetDataPresent(typeof(ViewModel.Person))) {
        var srcData = (ViewModel.Person)e.Data.GetData(typeof(ViewModel.Person));
        var destDataPeople = dataContext as ViewModel.People;
        var destDataPeopleGroup = dataContext as ViewModel.PeopleGroup;

        if (srcData == null || (destDataPeople != null && srcData.PeopleGroupId == null) ||
            (destDataPeopleGroup != null && destDataPeopleGroup.Id == srcData.PeopleGroupId) ||
            (destDataPeople == null && destDataPeopleGroup == null)) {
          e.Effects = DragDropEffects.None;
          e.Handled = true;
        }
      }
    }

    private void TvKeywords_OnDrop(object sender, DragEventArgs e) {
      var panel = (StackPanel)sender;

      if (e.Data.GetDataPresent(typeof (ViewModel.Keyword))) {
        var srcData = (ViewModel.Keyword) e.Data.GetData(typeof (ViewModel.Keyword));
        var destData = (ViewModel.Keyword) panel.DataContext;
        if (srcData == null || destData == null) return;
        var items = destData.Parent.Items;
        var destIndex = items.IndexOf(destData);
        var srcIndex = items.IndexOf(srcData);
        var dropOnTop = e.GetPosition(panel).Y < panel.ActualHeight/2;
        int newIndex;
        if (srcIndex > destIndex) {
          newIndex = dropOnTop ? destIndex : destIndex + 1;
        } else {
          newIndex = dropOnTop ? destIndex - 1 : destIndex;
        }
        items.Move(items.IndexOf(srcData), newIndex);

        for (var i = 0; i < items.Count; i++) {
          items[i].Index = i;
          items[i].Data.Idx = i;
          ACore.Db.Keywords.Context.SubmitChanges();
        }
      } else if (e.Data.GetDataPresent(typeof (ViewModel.Person))) {
        var srcData = (ViewModel.Person)e.Data.GetData(typeof(ViewModel.Person));
        var destData = panel.DataContext as ViewModel.PeopleGroup;
        if (srcData == null) return;
        ACore.People.MovePerson(srcData, destData);
      }
    }

    private void TvFolders_OnMouseMove(object sender, MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return;
      Vector diff = _dragDropStartPosition - e.GetPosition(null);
      if (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) &&
          !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return;
      var stackPanel = e.OriginalSource as StackPanel;
      if (stackPanel == null) return;
      DragDrop.DoDragDrop(stackPanel, stackPanel.DataContext, DragDropEffects.All);
    }

    private void TvFolders_AllowDropCheck(object sender, DragEventArgs e) {
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
        ACore.FileOperation(AppCore.FileOperations.Copy, from, destData.FullPath, itemName) : 
        ACore.FileOperation(AppCore.FileOperations.Move, from, destData.FullPath, itemName);
      if (!flag) return;

      if (thumbs) {
        if (e.KeyStates != DragDropKeyStates.ControlKey) {
          ACore.MediaItems.RemoveSelectedFromWeb();
          ACore.UpdateStatusBarInfo();
        }
        return;
      }

      if (e.KeyStates != DragDropKeyStates.ControlKey) {
        srcData.UpdateFullPath(srcData.Parent.FullPath, destData.FullPath);
        srcData.Parent.Items.Remove(srcData);
        srcData.Parent = destData;
        destData.Items.Add(srcData);
      } else {
        destData.GetSubFolders(true);
      }
    }

    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      //this is PreviewMouseRightButtonDown on StackPanel in TreeView
      e.Handled = true;
      StackPanel stackPanel = (StackPanel) sender;
      object item = stackPanel.DataContext;

      //if (stackPanel.ContextMenu != null) return;
      ContextMenu menu = new ContextMenu {Tag = item};

      switch (item.GetType().Name) {
        case nameof(ViewModel.Folder): {
          menu.Items.Add(new MenuItem {Command = (ICommand)Resources["FolderNew"], CommandParameter = item});
          if (((ViewModel.Folder) item).Parent != null) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRename"], CommandParameter = item});
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderDelete"], CommandParameter = item});
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderAddToFavorites"], CommandParameter = item});
          }
          break;
        }
        case nameof(ViewModel.FavoriteFolder): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["FolderRemoveFromFavorites"], CommandParameter = item});
          break;
        }
        case nameof(ViewModel.FolderKeyword): {
          if (!ACore.KeywordsEditMode) {
            menu.Items.Add(new MenuItem { Command = (ICommand)Resources["FolderKeywordShowAll"], CommandParameter = item });
          }
          break;
        }
        case nameof(ViewModel.Keyword): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordNew"], CommandParameter = item});
          if (((ViewModel.Keyword) item).Items.Count == 0) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordRename"], CommandParameter = item});
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordDelete"], CommandParameter = item});
          }
          if (!ACore.KeywordsEditMode) {
            menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordShowAll"], CommandParameter = item});
          }
          break;
        }
        case nameof(ViewModel.Keywords): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["KeywordNew"], CommandParameter = item});
          break;
        }
        case nameof(ViewModel.Person): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["PersonRename"], CommandParameter = item});
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["PersonDelete"], CommandParameter = item});
          break;
        }
        case nameof(ViewModel.People): {
          menu.Items.Add(new MenuItem {Command = (ICommand) Resources["PersonNew"], CommandParameter = item});
          menu.Items.Add(new MenuItem { Command = (ICommand)Resources["PeopleGroupNew"], CommandParameter = item });
          break;
        }
        case nameof(ViewModel.PeopleGroup): {
          menu.Items.Add(new MenuItem { Command = (ICommand)Resources["PersonNew"], CommandParameter = item });
          menu.Items.Add(new MenuItem { Command = (ICommand)Resources["PeopleGroupRename"], CommandParameter = item });
          menu.Items.Add(new MenuItem { Command = (ICommand)Resources["PeopleGroupDelete"], CommandParameter = item });
          break;
        }
        case nameof(ViewModel.Filters): {
          menu.Items.Add(new MenuItem { Command = (ICommand)Resources["FilterNew"], CommandParameter = item });
          break;
        }
        case nameof(ViewModel.Filter): {
          menu.Items.Add(new MenuItem { Command = (ICommand)Resources["FilterNew"], CommandParameter = item });
          menu.Items.Add(new MenuItem { Command = (ICommand)Resources["FilterEdit"], CommandParameter = item });
          menu.Items.Add(new MenuItem { Command = (ICommand)Resources["FilterDelete"], CommandParameter = item });
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
  }
}
