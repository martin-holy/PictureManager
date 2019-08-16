using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WMain.xaml
  /// </summary>
  public partial class WMain {
    private readonly string _argPicFile;
    private Point _dragDropStartPosition;
    private object _dragDropObject;
    private readonly System.Timers.Timer _presentationTimer;
    private bool _presentationTimerPaused;
    private bool _mainTreeViewIsPinnedInViewer;
    private bool _mainTreeViewIsPinnedInBrowser = true;
    private const int PresentationInterval = 3000;

    public AppCore ACore { get; set; }

    public WMain(string picFile) {
      ACore = new AppCore();
      Application.Current.Properties[nameof(AppProperty.AppCore)] = ACore;
      Application.Current.Properties[nameof(AppProperty.WMain)] = this;
      InitializeComponent();
      AddCommandBindings();
      AddInputBindings();

      _presentationTimer = new System.Timers.Timer();
      _presentationTimer.Elapsed += (o, e) => {
        if (_presentationTimer.Interval == 1) _presentationTimer.Interval = PresentationInterval;
        Application.Current.Dispatcher.Invoke(delegate {
          if (CanMediaItemNext())
            MediaItemNext();
          else
            _presentationTimer.Enabled = false;
        });
      };

      /*var ver = Assembly.GetEntryAssembly().GetName().Version;
      Title = $"{Title} {ver.Major}.{ver.Minor}";*/

      _argPicFile = picFile;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      ACore.Init();
      ACore.AppInfo.ProgressBarValue = 100;
      ACore.Folders.IsExpanded = true;
      MenuViewers.Header = ACore.CurrentViewer?.Title ?? "Viewer";

      App.SplashScreen.LoadComplete();
      Activate();

      if (!File.Exists(_argPicFile)) {
        ACore.AppInfo.AppMode = AppMode.Browser;
        return;
      }

      //app opened with argument
      // TODO
      /*ACore.AppInfo.AppMode = AppMode.Viewer;
      ACore.MediaItems.Load(ACore.Folders.ExpandTo(Path.GetDirectoryName(_argPicFile)), false);
      ACore.MediaItems.Current = ACore.MediaItems.Items.SingleOrDefault(x => x.FilePath.Equals(_argPicFile));
      if (ACore.MediaItems.Current != null) ACore.MediaItems.Current.IsSelected = true;
      SwitchToFullScreen();
      ACore.LoadThumbnails();*/
    }

    private void StartPresentationTimer(bool delay) {
      if (ACore.AppInfo.AppMode != AppMode.Viewer) return;
      _presentationTimer.Interval = delay ? PresentationInterval : 1;
      _presentationTimer.Enabled = true;
    }

    //this is PreviewMouseRightButtonDown on StackPanel in TreeView
    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      e.Handled = true;
      var stackPanel = (StackPanel)sender;
      if (stackPanel.ContextMenu != null) return;

      var item = stackPanel.DataContext;
      var menu = new ContextMenu { Tag = item };

      if ((item as ViewModel.BaseTreeViewItem)?.GetTopParent() is ViewModel.BaseCategoryItem category) {

        if (item is ViewModel.BaseCategoryItem && category.Category == Category.GeoNames) {
          menu.Items.Add(new MenuItem {Command = Commands.GeoNameNew, CommandParameter = item});
        }

        if (category.CanModifyItems) {
          var cat = item as ViewModel.BaseCategoryItem;
          var group = item as Database.CategoryGroup;

          if (cat != null || group != null || category.CanHaveSubItems) {
            menu.Items.Add(new MenuItem { Command = Commands.TagItemNew, CommandParameter = item });
          }

          if (item is ViewModel.BaseTreeViewTagItem && group == null || item is Database.Viewer) {
            menu.Items.Add(new MenuItem { Command = Commands.TagItemRename, CommandParameter = item });
            menu.Items.Add(new MenuItem { Command = Commands.TagItemDelete, CommandParameter = item });
          }

          if (category.CanHaveGroups && cat != null) {
            menu.Items.Add(new MenuItem { Command = Commands.CategoryGroupNew, CommandParameter = item });
          }

          if (group != null) {
            menu.Items.Add(new MenuItem { Command = Commands.CategoryGroupRename, CommandParameter = item });
            menu.Items.Add(new MenuItem { Command = Commands.CategoryGroupDelete, CommandParameter = item });
          }
        }
      }

      switch (item) {
        case Database.Folder folder: {
          menu.Items.Add(new MenuItem { Command = Commands.FolderNew, CommandParameter = item });
          if (folder.Parent != null) {
            menu.Items.Add(new MenuItem { Command = Commands.FolderRename, CommandParameter = item });
            menu.Items.Add(new MenuItem { Command = Commands.FolderDelete, CommandParameter = item });
            menu.Items.Add(new MenuItem { Command = Commands.FolderAddToFavorites, CommandParameter = item });
            menu.Items.Add(new MenuItem { Command = Commands.FolderSetAsFolderKeyword, CommandParameter = item });
          }
          break;
        }
        case Database.FavoriteFolder _: {
          menu.Items.Add(new MenuItem { Command = Commands.FolderRemoveFromFavorites, CommandParameter = item });
          break;
        }
        case Database.Viewer _: {
          menu.Items.Add(new MenuItem { Command = Commands.ViewerIncludeFolder, CommandParameter = item });
          menu.Items.Add(new MenuItem { Command = Commands.ViewerExcludeFolder, CommandParameter = item });
            break;
        }
        case Database.Person _:
        case Database.Keyword _:
        case Database.GeoName _: {
          menu.Items.Add(new MenuItem { Command = Commands.MediaItemsLoadByTag, CommandParameter = item });
          break;
        }
        case ViewModel.FolderKeywords _: {
          menu.Items.Add(new MenuItem {Command = Commands.OpenFolderKeywordsList});
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
        (Keyboard.Modifiers & ModifierKeys.Control) > 0,
        (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
        (Keyboard.Modifiers & ModifierKeys.Shift) > 0);
    }

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
      if (!(sender is StackPanel stackPanel)) return;
      _dragDropObject = stackPanel.DataContext;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void TvFolders_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      _dragDropObject = null;
    }

    private void TvFolders_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      if (!(e.OriginalSource is StackPanel stackPanel) || _dragDropObject == null) return;
      DragDrop.DoDragDrop(stackPanel, _dragDropObject, DragDropEffects.Move | DragDropEffects.Copy);
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
      var srcData = (Database.Folder)e.Data.GetData(typeof(Database.Folder));
      var destData = (Database.Folder)((StackPanel)sender).DataContext;
      if ((srcData == null && !thumbs) || destData == null || srcData == destData || !destData.IsAccessible) {
        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
    }

    private void TvFolders_OnDrop(object sender, DragEventArgs e) {
      var thumbs = e.Data.GetDataPresent(DataFormats.FileDrop); //thumbnails drop
      var srcFolder = (Database.Folder) e.Data.GetData(typeof(Database.Folder));
      var destFolder = (Database.Folder) ((StackPanel) sender).DataContext;
      var items = thumbs ? ACore.MediaItems.Items.Where(x => x.IsSelected).ToList() : null;
      var foMode = e.KeyStates == DragDropKeyStates.ControlKey ? FileOperationMode.Copy : FileOperationMode.Move;

      if (items != null) { // MediaItems
        ACore.MediaItems.CopyMove(foMode, items, destFolder);
        ACore.MediaItems.Helper.IsModifed = true;
      }
      else { // Folder
        ACore.Folders.CopyMove(foMode, srcFolder, destFolder);
        ACore.MediaItems.Helper.IsModifed = true;
        ACore.Folders.Helper.IsModifed = true;
        ACore.FolderKeywords.Load();
      }

      ACore.Sdb.SaveAllTables();

      // reload last selected source if was moved
      if (foMode == FileOperationMode.Move && srcFolder == ACore.LastSelectedSource) {
        ACore.MediaItems.Current = null;
        ACore.UpdateStatusBarInfo();

        var folder = destFolder.GetByPath(srcFolder?.Title);
        if (folder == null) return;
        ACore.Folders.ExpandTo(folder);
        ACore.TreeView_Select(folder, false, false, false);
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
      if (!(sender is StackPanel stackPanel)) return;
      _dragDropObject = stackPanel.DataContext;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void TvKeywords_OnMouseLeftButtonUp(object sender, MouseEventArgs e) {
      _dragDropObject = null;
    }

    private void TvKeywords_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      if (!(e.OriginalSource is StackPanel stackPanel) || _dragDropObject == null) return;
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
      if (e.Data.GetDataPresent(typeof(Database.Keyword))) {

        if (((dest as ViewModel.BaseCategoryItem)?.Category ?? (dest as Database.CategoryGroup)?.Category) == Category.Keywords)
          return;

        var srcData = e.Data.GetData(typeof(Database.Keyword)) as Database.Keyword;
        var destData = dest as Database.Keyword;
        if (destData?.Parent == srcData?.Parent) return;

        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
      else if (e.Data.GetDataPresent(typeof(Database.Person))) {
        if (((dest as ViewModel.BaseCategoryItem)?.Category ?? (dest as Database.CategoryGroup)?.Category) == Category.People) {
          var srcData = (Database.Person)e.Data.GetData(typeof(Database.Person));
          if (srcData != null && srcData.Parent != (ViewModel.BaseTreeViewItem)dest) return;
        }
        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
    }

    private void TvKeywords_OnDrop(object sender, DragEventArgs e) {
      var panel = (StackPanel) sender;
      if (!(panel.DataContext is ViewModel.BaseTreeViewItem destData)) return;

      if (e.Data.GetDataPresent(typeof(Database.Keyword))) {
        var srcData = (Database.Keyword)e.Data.GetData(typeof(Database.Keyword));
        if (srcData == null) return;
        var dropOnTop = e.GetPosition(panel).Y < panel.ActualHeight / 2;
        ACore.Keywords.ItemMove(srcData, destData, dropOnTop);
      }
      else if (e.Data.GetDataPresent(typeof(Database.Person))) {
        var srcData = (Database.Person)e.Data.GetData(typeof(Database.Person));
        if (srcData == null) return;
        ACore.People.ItemMove(srcData, destData);
      }
    }
    #endregion

    #region Thumbnail
    private void Thumb_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var bmi = (Database.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext;

      if (!isCtrlOn && !isShiftOn) {
        ACore.MediaItems.DeselectAll();
        ACore.MediaItems.Current = bmi;
      }
      else {
        if (isCtrlOn) bmi.IsSelected = !bmi.IsSelected;
        if (isShiftOn && ACore.MediaItems.Current != null) {
          var from = ACore.MediaItems.Current.Index;
          var to = bmi.Index;
          if (from > to) {
            to = from;
            from = bmi.Index;
          }

          for (var i = from; i < to + 1; i++) {
            ACore.MediaItems.Items[i].IsSelected = true;
          }
        }
      }

      ACore.UpdateStatusBarInfo();
      ACore.MarkUsedKeywordsAndPeople();
    }

    private void Thumb_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      _dragDropStartPosition = e.GetPosition(null);
      if (e.ClickCount != 2) return;
      ACore.MediaItems.DeselectAll();
      ACore.MediaItems.Current = (Database.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext;
      SwitchToFullScreen();
      SetMediaItemSource();
    }

    private void Thumb_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      var dob = new DataObject();
      var data = ACore.MediaItems.Items.Where(x => x.IsSelected).Select(p => p.FilePath).ToList();
      if (data.Count == 0)
        data.Add(((Database.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext).FilePath);
      dob.SetData(DataFormats.FileDrop, data.ToArray());
      DragDrop.DoDragDrop(this, dob, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void ThumbsBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
      if (e.Delta < 0 && ACore.ThumbScale < .1) return;
      ACore.ThumbScale += e.Delta > 0 ? .05 : -.05;
      ACore.AppInfo.IsThumbInfoVisible = ACore.ThumbScale > 0.5;
      ACore.MediaItems.SplitedItemsReload();
      ACore.MediaItems.ResetThumbsSize();
    }

    #endregion

    private void MediaItemSize_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      ACore.MediaItemSizes.Size.SliderChanged = true;
      ACore.TreeView_Select(ACore.LastSelectedSource, false, false, ACore.LastSelectedSourceRecursive);
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    private void SetMediaItemSource() {
      var current = ACore.MediaItems.Current;
      switch (current.MediaType) {
        case MediaType.Image: {
          FullImage.SetSource(current);
          FullMedia.Source = null;
          break;
        }
        case MediaType.Video: {
          var isBigger = FullMedia.ActualHeight < current.Height ||
                         FullMedia.ActualWidth < current.Width;
          FullMedia.Stretch = isBigger ? Stretch.Uniform : Stretch.None;
          FullMedia.Source = current.FilePathUri;
          break;
        }
      }
    }

    private void PanelFullScreen_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount == 2) {
        SwitchToBrowser();
      }
    }

    private void PanelFullScreen_OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) return;
      if (e.Delta < 0) {
        if (CanMediaItemNext())
          MediaItemNext();
      }
      else {
        if (CanMediaItemPrevious())
          MediaItemPrevious();
      }
    }

    private void FullMedia_OnMediaEnded(object sender, RoutedEventArgs e) {
      if (_presentationTimerPaused) {
        _presentationTimerPaused = false;
        StartPresentationTimer(false);
      }
      else {
        FullMedia.Stop();
        FullMedia.Rewind();
        FullMedia.Play();
      }
    }

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
      catch (Exception ex) {
        return false;
      }

      try {
        temp.CreationTime = original.CreationTime;
        original.Delete();
        temp.MoveTo(original.FullName);
      }
      catch (Exception ex) {
        return false;
      }

      return true;
    }

    private void TestButton() {
      var tests = new Tests(ACore);
    }

    private void WMain_OnMouseMove(object sender, MouseEventArgs e) {
      var pos = e.GetPosition(this);
      if (pos.X < 3 && !FlyoutMainTreeView.IsOpen)
        FlyoutMainTreeView.IsOpen = true;
    }

    private void FlyoutMainTreeView_OnMouseLeave(object sender, MouseEventArgs e) {
      if (!FlyoutMainTreeView.IsPinned)
        FlyoutMainTreeView.IsOpen = false;
    }

    private void MainSplitter_OnDragDelta(object sender, DragDeltaEventArgs e) {
      FlyoutMainTreeView.Width = GridMain.ColumnDefinitions[0].ActualWidth;
      ACore.MediaItems.SplitedItemsReload();
      ACore.MediaItems.ScrollTo(ACore.MediaItems.Current?.Index ?? 0);
    }

    private void MainSplitter_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      MainSplitter_OnDragDelta(null, null);
    }

    private void WMain_OnClosing(object sender, CancelEventArgs e) {
      ACore.Sdb.SaveAllTables();
    }
  }
}
