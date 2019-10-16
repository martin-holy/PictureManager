﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PictureManager.ViewModel;
using PictureManager.Database;
using PictureManager.Dialogs;

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

    public WMain(string picFile) {
      InitializeComponent();
      FullMedia.MediaElement.MediaEnded += FullMedia_OnMediaEnded;

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

    ~WMain() {
      _presentationTimer?.Dispose();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      App.Core.Init();
      App.Core.AppInfo.ProgressBarValue = 100;
      App.Core.Folders.IsExpanded = true;
      MenuViewers.Header = App.Core.CurrentViewer?.Title ?? "Viewer";

      App.SplashScreen.LoadComplete();
      Activate();

      if (!File.Exists(_argPicFile)) {
        App.Core.AppInfo.AppMode = AppMode.Browser;
        //return;
      }

      //app opened with argument
      // TODO
      /*App.Core.AppInfo.AppMode = AppMode.Viewer;
      App.Core.MediaItems.Load(App.Core.Folders.ExpandTo(Path.GetDirectoryName(_argPicFile)), false);
      App.Core.MediaItems.Current = App.Core.MediaItems.Items.SingleOrDefault(x => x.FilePath.Equals(_argPicFile));
      if (App.Core.MediaItems.Current != null) App.Core.MediaItems.Current.IsSelected = true;
      SwitchToFullScreen();
      App.Core.LoadThumbnails();*/
    }

    private void StartPresentationTimer(bool delay) {
      if (App.Core.AppInfo.AppMode != AppMode.Viewer) return;
      _presentationTimer.Interval = delay ? PresentationInterval : 1;
      _presentationTimer.Enabled = true;
    }

    //this is PreviewMouseRightButtonDown on StackPanel in TreeView
    private void AttachContextMenu(object sender, MouseButtonEventArgs e) {
      e.Handled = true;
      var stackPanel = (StackPanel)sender;
      if (stackPanel.ContextMenu != null) return;

      var item = stackPanel.DataContext;
      var menu = new ContextMenu {Tag = item};
      var binding = new Binding("PlacementTarget") {
        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ContextMenu), 1)
      };

      void AddMenuItem(ICommand command) {
        var menuItem = new MenuItem {Command = command, CommandParameter = item};
        menuItem.SetBinding(MenuItem.CommandTargetProperty, binding);
        menu.Items.Add(menuItem);
      }

      if ((item as BaseTreeViewItem)?.GetTopParent() is BaseCategoryItem category) {

        if (item is BaseCategoryItem && category.Category == Category.GeoNames)
          AddMenuItem(Commands.GeoNameNew);

        if (category.CanModifyItems) {
          var cat = item as BaseCategoryItem;
          var group = item as CategoryGroup;

          if (cat != null || group != null || category.CanHaveSubItems) {
            AddMenuItem(Commands.TagItemNew);
            AddMenuItem(Commands.TagItemDeleteNotUsed);
          }

          if (item is BaseTreeViewTagItem && group == null || item is Viewer) {
            AddMenuItem(Commands.TagItemRename);
            AddMenuItem(Commands.TagItemDelete);
          }

          if (category.CanHaveGroups && cat != null)
            AddMenuItem(Commands.CategoryGroupNew);

          if (group != null) {
            AddMenuItem(Commands.CategoryGroupRename);
            AddMenuItem(Commands.CategoryGroupDelete);
          }
        }
      }

      switch (item) {
        case Folder folder: {
          AddMenuItem(Commands.FolderNew);

          if (folder.Parent != null) {
            AddMenuItem(Commands.FolderRename);
            AddMenuItem(Commands.FolderDelete);
            AddMenuItem(Commands.FolderAddToFavorites);
          }

          AddMenuItem(Commands.FolderSetAsFolderKeyword);
          AddMenuItem(Commands.MetadataReload2);
          AddMenuItem(Commands.MediaItemsRebuildThumbnails);
          break;
        }
        case FavoriteFolder _: {
          AddMenuItem(Commands.FolderRemoveFromFavorites);
          break;
        }
        case Viewer _: {
          AddMenuItem(Commands.ViewerIncludeFolder);
          AddMenuItem(Commands.ViewerExcludeFolder);
          break;
        }
        case Person _:
        case Keyword _:
        case GeoName _: {
          AddMenuItem(Commands.MediaItemsLoadByTag);
          break;
        }
        case FolderKeywords _: {
          AddMenuItem(Commands.OpenFolderKeywordsList);
          break;
        }
        case BaseTreeViewItem bti: {
          if (bti.Parent?.Parent is Viewer)
            AddMenuItem(Commands.ViewerRemoveFolder);
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
      App.Core.TreeView_Select(((StackPanel)sender).DataContext as BaseTreeViewItem, 
        (Keyboard.Modifiers & ModifierKeys.Control) > 0,
        (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
        (Keyboard.Modifiers & ModifierKeys.Shift) > 0);
    }

    private void TreeView_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (!(sender is StackPanel stackPanel)) return;
      _dragDropObject = stackPanel.DataContext;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void TreeView_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      if (!(e.OriginalSource is StackPanel stackPanel) || _dragDropObject == null) return;
      DragDrop.DoDragDrop(stackPanel, _dragDropObject, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void TreeView_OnDrop(object sender, DragEventArgs e) {
      var panel = (StackPanel) sender;
      if (!(panel.DataContext is BaseTreeViewItem destData)) return;

      // MediaItems
      if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
        var foMode = e.KeyStates == DragDropKeyStates.ControlKey ? FileOperationMode.Copy : FileOperationMode.Move;
        App.Core.MediaItems.CopyMove(
          foMode, App.Core.MediaItems.FilteredItems.Where(x => x.IsSelected).ToList(), (Folder) destData);
        App.Core.MediaItems.Helper.IsModifed = true;
      }
      // Folder
      else if (e.Data.GetDataPresent(typeof(Folder))) {
        var foMode = e.KeyStates == DragDropKeyStates.ControlKey ? FileOperationMode.Copy : FileOperationMode.Move;
        var srcData = (Folder) e.Data.GetData(typeof(Folder));
        if (srcData == null) return;

        App.Core.Folders.CopyMove(foMode, srcData, (Folder) destData);
        App.Core.MediaItems.Helper.IsModifed = true;
        App.Core.Folders.Helper.IsModifed = true;
        App.Core.FolderKeywords.Load();

        // reload last selected source if was moved
        if (foMode == FileOperationMode.Move && srcData.IsSelected) {
          var folder = ((Folder) destData).GetByPath(srcData.Title);
          if (folder == null) return;
          BaseTreeViewItem.ExpandTo(folder);
          App.Core.TreeView_Select(folder, false, false, false);
        }
      }
      // Keyword
      else if (e.Data.GetDataPresent(typeof(Keyword))) {
        var srcData = (Keyword) e.Data.GetData(typeof(Keyword));
        if (srcData == null) return;
        var dropOnTop = e.GetPosition(panel).Y < panel.ActualHeight / 2;
        App.Core.Keywords.ItemMove(srcData, destData, dropOnTop);
      }
      // Person
      else if (e.Data.GetDataPresent(typeof(Person))) {
        var srcData = (Person) e.Data.GetData(typeof(Person));
        if (srcData == null) return;
        App.Core.People.ItemMove(srcData, destData);
      }

      App.Core.Sdb.SaveAllTables();
    }

    private void TreeView_AllowDropCheck(object sender, DragEventArgs e) {
      // scroll treeview when the mouse is near the top or bottom
      var treeView = ((StackPanel) sender).TryFindParent<TreeView>();
      if (treeView != null) {
        var border = VisualTreeHelper.GetChild(treeView, 0);
        if (VisualTreeHelper.GetChild(border, 0) is ScrollViewer scrollViewer) {
          var pos = e.GetPosition(treeView);
          if (pos.Y < 25) {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 25);
          }
          else if (treeView.ActualHeight - pos.Y < 25) {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 25);
          }
        }
      }

      // return if the data can be droped
      var dataContext = ((StackPanel) sender).DataContext;

      // MediaItems
      if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
        var dragged = ((string[]) e.Data.GetData(DataFormats.FileDrop))?.OrderBy(x => x).ToArray();
        var selected = App.Core.MediaItems.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).OrderBy(p => p).ToArray();

        if (dragged != null && selected.SequenceEqual(dragged) && 
            dataContext is Folder destData && destData.IsAccessible) return;
      }
      else // Folder
      if (e.Data.GetDataPresent(typeof(Folder))) {
        var srcData = (Folder)e.Data.GetData(typeof(Folder));

        if (srcData != null && dataContext is Folder destData && !destData.HasThisParent(srcData) && 
            srcData != destData && destData.IsAccessible && (Folder)srcData.Parent != destData) return;
      }
      else // Keyword
      if (e.Data.GetDataPresent(typeof(Keyword))) {
        if (dataContext is BaseTreeViewItem destData &&
            (destData.GetTopParent() as BaseCategoryItem)?.Category == Category.Keywords) return;
      }
      else // Person
      if (e.Data.GetDataPresent(typeof(Person))) {
        var srcData = (Person) e.Data.GetData(typeof(Person));

        if (dataContext is BaseTreeViewItem destData && srcData?.Parent != destData &&
            (destData.GetTopParent() as BaseCategoryItem)?.Category == Category.People) return;
      }

      // can't be droped
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    #region Thumbnail
    private void Thumb_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var mi = (MediaItem) ((FrameworkElement) sender).DataContext;

      App.Core.MediaItems.Select(isCtrlOn, isShiftOn, mi);
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private void Thumb_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      _dragDropStartPosition = e.GetPosition(null);
      if (e.ClickCount != 2) return;
      App.Core.MediaItems.DeselectAll();
      App.Core.MediaItems.Current = (MediaItem) ((Grid) ((Border) sender).Child).DataContext;
      SwitchToFullScreen();
      SetMediaItemSource();
    }

    private void Thumb_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      var dob = new DataObject();
      var data = App.Core.MediaItems.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).ToList();
      if (data.Count == 0)
        data.Add(((MediaItem) ((Grid) ((Border) sender).Child).DataContext).FilePath);
      dob.SetData(DataFormats.FileDrop, data.ToArray());
      DragDrop.DoDragDrop(this, dob, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void ThumbsBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
      if (e.Delta < 0 && App.Core.ThumbScale < .1) return;
      App.Core.ThumbScale += e.Delta > 0 ? .05 : -.05;
      App.Core.AppInfo.IsThumbInfoVisible = App.Core.ThumbScale > 0.5;
      App.Core.MediaItems.SplitedItemsReload();
      App.Core.MediaItems.ResetThumbsSize();
    }

    #endregion

    private void MediaItemSize_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      App.Core.MediaItemSizes.Size.SliderChanged = true;
      App.Core.MediaItems.ReapplyFilter();
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    private void SetMediaItemSource() {
      var current = App.Core.MediaItems.Current;
      switch (current.MediaType) {
        case MediaType.Image: {
          FullImage.SetSource(current);
          FullMedia.MediaElement.Source = null;
          FullMedia.IsPlaying = false;
          break;
        }
        case MediaType.Video: {
          var isBigger = FullMedia.ActualHeight < current.Height ||
                         FullMedia.ActualWidth < current.Width;
          FullMedia.MediaElement.Stretch = isBigger ? Stretch.Uniform : Stretch.None;
          FullMedia.MediaElement.Source = current.FilePathUri;
          FullMedia.IsPlaying = true;
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
        FullMedia.MediaElement.Stop();
        FullMedia.MediaElement.Play();
      }
    }

    private static void TestButton() {
      var tests = new Tests();
      tests.Run();
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
      App.Core.MediaItems.SplitedItemsReload();
      App.Core.MediaItems.ScrollToCurrent();
    }

    private void MainSplitter_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      MainSplitter_OnDragDelta(null, null);
    }

    private void WMain_OnClosing(object sender, CancelEventArgs e) {
      if (App.Core.MediaItems.ModifedItems.Count > 0 &&
          MessageDialog.Show("Metadata Edit", "Some Media Items are modifed, do you want to save them?", true)) {
        MetadataSave();
      }
      App.Core.Sdb.SaveAllTables();
    }

    private void WMain_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      _dragDropObject = null;
    }
  }
}
