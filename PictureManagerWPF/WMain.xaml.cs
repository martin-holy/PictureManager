using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

    public WMain(string picFile) {
      ACore = new AppCore();
      Application.Current.Properties[nameof(AppProperty.AppCore)] = ACore;
      Application.Current.Properties[nameof(AppProperty.WMain)] = this;
      InitializeComponent();
      AddCommandBindings();
      AddInputBindings();

      _presentationTimer = new System.Timers.Timer(3000);
      _presentationTimer.Elapsed += (o, e) => {
        Application.Current.Dispatcher.Invoke(delegate {
          if (CanMediaItemNext())
            MediaItemNext();
        });
      };

      var ver = Assembly.GetEntryAssembly().GetName().Version;
      Title = $"{Title} {ver.Major}.{ver.Minor}";

      _argPicFile = picFile;
    }

    public AppCore ACore { get; set; }

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
      ACore.AppInfo.AppMode = AppMode.Viewer;
      ACore.MediaItems.Load(ACore.Folders.ExpandTo(Path.GetDirectoryName(_argPicFile)), false);
      ACore.MediaItems.Current = ACore.MediaItems.Items.SingleOrDefault(x => x.FilePath.Equals(_argPicFile));
      if (ACore.MediaItems.Current != null) ACore.MediaItems.Current.IsSelected = true;
      SwitchToFullScreen();
      ACore.LoadThumbnails();
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
          var group = item as ViewModel.CategoryGroup;

          if (cat != null || group != null || category.CanHaveSubItems) {
            menu.Items.Add(new MenuItem { Command = Commands.TagItemNew, CommandParameter = item });
          }

          if (item is ViewModel.BaseTreeViewTagItem && group == null || item is ViewModel.Viewer) {
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
        case ViewModel.Folder folder: {
          menu.Items.Add(new MenuItem { Command = Commands.FolderNew, CommandParameter = item });
          if (folder.Parent != null) {
            menu.Items.Add(new MenuItem { Command = Commands.FolderRename, CommandParameter = item });
            menu.Items.Add(new MenuItem { Command = Commands.FolderDelete, CommandParameter = item });
            menu.Items.Add(new MenuItem { Command = Commands.FolderAddToFavorites, CommandParameter = item });
          }
          break;
        }
        case ViewModel.FavoriteFolder _: {
          menu.Items.Add(new MenuItem { Command = Commands.FolderRemoveFromFavorites, CommandParameter = item });
          break;
        }
        case ViewModel.Filters _: {
          menu.Items.Add(new MenuItem { Command = Commands.FilterNew, CommandParameter = item });
          break;
        }
        case ViewModel.Filter _: {
          menu.Items.Add(new MenuItem { Command = Commands.FilterNew, CommandParameter = item });
          menu.Items.Add(new MenuItem { Command = Commands.FilterEdit, CommandParameter = item });
          menu.Items.Add(new MenuItem { Command = Commands.FilterDelete, CommandParameter = item });
          break;
        }
        case ViewModel.Viewer _: {
          menu.Items.Add(new MenuItem { Command = Commands.ViewerIncludeFolder, CommandParameter = item });
          menu.Items.Add(new MenuItem { Command = Commands.ViewerExcludeFolder, CommandParameter = item });
            break;
        }
        case ViewModel.Person _:
        case ViewModel.Keyword _:
        case ViewModel.GeoName _: {
          menu.Items.Add(new MenuItem { Command = Commands.MediaItemsLoadByTag, CommandParameter = item });
          break;
        }
        case ViewModel.BaseTreeViewItem btvi: {
          if (btvi.Tag is DataModel.ViewerAccess)
            menu.Items.Add(new MenuItem { Command = Commands.ViewerRemoveFolder, CommandParameter = item });
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
        ACore.FileOperation(FileOperationMode.Copy, from, destData.FullPath, itemName) :
        ACore.FileOperation(FileOperationMode.Move, from, destData.FullPath, itemName);
      if (!flag) return;

      if (thumbs) {
        if (e.KeyStates != DragDropKeyStates.ControlKey) {
          ACore.MediaItems.RemoveSelected(false);
          ACore.MediaItems.Current = null;
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

        if (((dest as ViewModel.BaseCategoryItem)?.Category ?? (dest as ViewModel.CategoryGroup)?.Category) == Category.Keywords)
          return;

        var srcData = e.Data.GetData(typeof(ViewModel.Keyword)) as ViewModel.Keyword;
        var destData = dest as ViewModel.Keyword;
        if (destData?.Parent == srcData?.Parent) return;

        e.Effects = DragDropEffects.None;
        e.Handled = true;
      }
      else if (e.Data.GetDataPresent(typeof(ViewModel.Person))) {
        if (((dest as ViewModel.BaseCategoryItem)?.Category ?? (dest as ViewModel.CategoryGroup)?.Category) == Category.People) {
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

    #region Thumbnail
    private void Thumb_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var bmi = (ViewModel.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext;

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
      ACore.MediaItems.Current = (ViewModel.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext;
      SwitchToFullScreen();
      SetMediaItemSource();
    }

    private void Thumb_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      var dob = new DataObject();
      var data = ACore.MediaItems.Items.Where(x => x.IsSelected).Select(p => p.FilePath).ToList();
      if (data.Count == 0)
        data.Add(((ViewModel.BaseMediaItem) ((Grid) ((Border) sender).Child).DataContext).FilePath);
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
      Application.Current.Properties["MediaItemSizeSliderChanged"] = true;
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
          var isBigger = FullMedia.ActualHeight < current.Data.Height ||
                         FullMedia.ActualWidth < current.Data.Width;
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
      FullMedia.Stop();
      FullMedia.Rewind();
      FullMedia.Play();
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

    private void TestButton() {
      //var folder = new ViewModel.Folder { FullPath = @"d:\Pictures\01 Digital_Foto\-=Hotovo\2016" };
      //var fk = ACore.FolderKeywords.GetFolderKeywordByFullPath(folder.FullPath);
      //ACore.MediaItems.Load(folder, true);
      //ACore.MediaItems.Load(fk, true);
      //ACore.MediaItems.LoadByTag(fk, true);
      //ACore.MediaItems.LoadByFolder(folder.FullPath, true);
      //ACore.InitThumbsPagesControl();

      //ACore.MediaItems.LoadPeople(ACore.MediaItems.Items.ToList());


      //var file1 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\video.mp4");
      //var x = GetFileProps(@"d:\video.mp4");
      //var xx = ShellStuff.FileInformation.GetVideoMetadata(@"d:\video.mp4");

      /*var mediaItems = ACore.MediaItems.AllItems.Where(x => x.MediaType == MediaType.Image);
      var panoramas = new List<ViewModel.BaseMediaItem>();
      foreach (var mi in mediaItems) {
        mi.SetThumbSize();
        if (mi.ThumbWidth > 400 || mi.ThumbHeight > 400)
          panoramas.Add(mi);
      }

      foreach (var mi in panoramas) {
        if (File.Exists(mi.FilePath))
          AppCore.CreateThumbnail(mi.FilePath, mi.FilePathCache, mi.ThumbSize);
      }*/



      //height 309, width 311

      /*var file1 = ShellStuff.FileInformation.GetFileIdInfo(@"c:\20150831_114319_Martin.jpg");
      var file2 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\!test\20150831_114319_Martin.jpg");
      var file3 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\Temp\20150831_114319_Martin.jpg");
      //3659174697441353
      var filePath = @"d:\!test\20150831_114319_Martin.jpg";
      var fileInfo = new FileInfo(filePath);*/

      /*var formattedText = new FormattedText(
        "\U0001F4CF",
        CultureInfo.GetCultureInfo("en-us"),
        FlowDirection.LeftToRight,
        new Typeface("Segoe UI Symbol"),
        32,
        Brushes.Black);
      var buildGeometry = formattedText.BuildGeometry(new Point(0, 0));
      var p = buildGeometry.GetFlattenedPathGeometry();*/
    }

    private void TcMain_OnSizeChanged(object sender, SizeChangedEventArgs e) {
      /*ACore.MediaItems.SplitedItemsReload();
      ACore.MediaItems.ScrollTo(ACore.MediaItems.Current?.Index ?? 0);*/
    }
  }
}
