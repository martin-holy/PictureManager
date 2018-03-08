using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Controls.ContextMenu;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WMain.xaml
  /// </summary>
  public partial class WMain {
    private readonly string _argPicFile;
    private Point _dragDropStartPosition;
    private object _dragDropObject;

    public WMain(string picFile) {
      InitializeComponent();

      var ver = Assembly.GetEntryAssembly().GetName().Version;
      Title = $"{Title} {ver.Major}.{ver.Minor}";

      ACore = new AppCore(this);
      Application.Current.Properties[nameof(AppProps.AppCore)] = ACore;

      _argPicFile = picFile;
    }

    public AppCore ACore { get; set; }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      InitUi();

      App.SplashScreen.LoadComplete();
      Activate();

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
      ACore.LoadThumbnails();
    }

    public void InitUi() {
      ACore.Init();
      ACore.AppInfo.ProgressBarValue = 100;
      ACore.Folders.IsExpanded = true;
      TvFolders.ItemsSource = ACore.FoldersRoot;
      TvKeywords.ItemsSource = ACore.KeywordsRoot;
      TvFilters.ItemsSource = ACore.FiltersRoot;
      MenuViewers.ItemsSource = ACore.Viewers.Items;
      MenuViewers.Header = ACore.CurrentViewer?.Title ?? "Viewer";
    }

    public void SwitchToFullScreen() {
      if (ACore.MediaItems.Current == null) return;
      ACore.AppInfo.AppMode = AppModes.Viewer;
      UseNoneWindowStyle = true;
      IgnoreTaskbarOnMaximize = true;
      MainMenu.Visibility = Visibility.Hidden;
    }

    public void SwitchToBrowser() {
      ACore.AppInfo.AppMode = AppModes.Browser;
      ACore.MediaItems.ScrollToCurrent();
      ACore.MarkUsedKeywordsAndPeople();
      UseNoneWindowStyle = false;
      ShowTitleBar = true;
      IgnoreTaskbarOnMaximize = false;
      MainMenu.Visibility = Visibility.Visible;
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
          MenuAddItem(menu, "ViewerIncludeFolder", item);
          MenuAddItem(menu, "ViewerExcludeFolder", item);
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
      CmdKeywordsSave_Executed(null, null);
    }

    private void BtnStatBarCancel_OnClick(object sender, RoutedEventArgs e) {
      CmdKeywordsCancel_Executed(null, null);
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
        ACore.FileOperation(FileOperations.Copy, from, destData.FullPath, itemName) :
        ACore.FileOperation(FileOperations.Move, from, destData.FullPath, itemName);
      if (!flag) return;

      if (thumbs) {
        if (e.KeyStates != DragDropKeyStates.ControlKey) {
          ACore.MediaItems.RemoveSelected();
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

      //ACore.MediaItems.LoadPeople(ACore.MediaItems.Items.ToList());
      ACore.AppInfo.AppMode = AppModes.Viewer;

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

    private void Thumb_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      var isCtrlOn = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
      var isShiftOn = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
      var bmi = (ViewModel.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext;

      if (!isCtrlOn && !isShiftOn) {
        ACore.MediaItems.DeselectAll();
        ACore.MediaItems.Current = bmi;
      }
      else {
        if (isCtrlOn) bmi.IsSelected = !bmi.IsSelected;
        if (!isShiftOn || ACore.MediaItems.Current == null) return;
        var from = ACore.MediaItems.Current.Index;
        var to = bmi.Index;
        if (from > to) { to = from; from = bmi.Index; }
        for (var i = from; i < to + 1; i++) {
          ACore.MediaItems.Items[i].IsSelected = true;
        }
      }
      ACore.UpdateStatusBarInfo();
      ACore.MarkUsedKeywordsAndPeople();
    }

    private void Thumb_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      _dragDropStartPosition = e.GetPosition(null);
      if (e.ClickCount != 2) return;
      ACore.MediaItems.DeselectAll();
      ACore.MediaItems.Current = (ViewModel.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext;
      SetMediaItemSource();
      SwitchToFullScreen();
    }

    private void Thumb_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      var dob = new DataObject();
      var data = ACore.MediaItems.Items.Where(x => x.IsSelected).Select(p => p.FilePath).ToList();
      if (data.Count == 0)
        data.Add(((ViewModel.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext).FilePath);
      dob.SetData(DataFormats.FileDrop, data.ToArray());
      DragDrop.DoDragDrop(ACore.WMain, dob, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      if (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) &&
          !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)) return false;
      return true;
    }

    private void FullScreenBox_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount == 2) {
        if (FullMedia.Source != null) {
          FullMedia.Source = null;
          GC.Collect();
        }

        SwitchToBrowser();
      }
    }

    private void SetMediaItemSource() {
      switch (ACore.MediaItems.Current.MediaType) {
        case MediaTypes.Image: {
          FullImage.Orientation = (MediaOrientation)ACore.MediaItems.Current.Data.Orientation;
          FullImage.FilePath = ACore.MediaItems.Current.FilePath;
          FullMedia.Source = null;
          break;
        }
        case MediaTypes.Video: {
          FullMedia.Source = ACore.MediaItems.Current.FilePathUri;
          break;
        }
      }
    }

    private void FullScreenBox_OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) return;
      if (e.Delta < 0) {
        if (CmdMediaItemNext.CanExecute(null, null))
          CmdMediaItemNext.Execute(null, null);
      }
      else {
        if (CmdMediaItemPrevious.CanExecute(null, null))
          CmdMediaItemPrevious.Execute(null, null);
      }
    }


    private void TcMain_OnSizeChanged(object sender, SizeChangedEventArgs e) {
      /*ACore.MediaItems.SplitedItemsReload();
      ACore.MediaItems.ScrollTo(ACore.MediaItems.Current?.Index ?? 0);*/
    }


  }
}
