using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PictureManager.Commands;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.ViewModels;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WMain.xaml
  /// </summary>
  public partial class WMain {
    private Point _dragDropStartPosition;
    private object _dragDropObject;

    public MediaElement VideoThumbnailPreview;

    public CommandsController CommandsController => CommandsController.Instance;

    #region DependencyProperties
    public static readonly DependencyProperty FlyoutMainTreeViewMarginProperty = DependencyProperty.Register(
      nameof(FlyoutMainTreeViewMargin), typeof(Thickness), typeof(WMain));

    public Thickness FlyoutMainTreeViewMargin {
      get => (Thickness) GetValue(FlyoutMainTreeViewMarginProperty);
      set => SetValue(FlyoutMainTreeViewMarginProperty, value);
    }
    #endregion

    public WMain() {
      InitializeComponent();

      PresentationPanel.Elapsed = delegate {
        Application.Current.Dispatcher?.Invoke(delegate {
          if (MediaItemsCommands.CanNext())
            CommandsController.MediaItemsCommands.Next();
          else
            PresentationPanel.Stop();
        });
      };

      FullMedia.RepeatEnded += delegate {
        if (!PresentationPanel.IsPaused) return;
        PresentationPanel.Start(false);
      };

      VideoThumbnailPreview = new MediaElement {
        LoadedBehavior = MediaState.Manual,
        IsMuted = true,
        Stretch = Stretch.Fill
      };

      VideoThumbnailPreview.MediaEnded += (o, args) => {
        // MediaElement.Stop()/Play() doesn't work when is video shorter than 1s
        ((MediaElement) o).Position = TimeSpan.FromMilliseconds(1);
      };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) {
      CommandsController.AddCommandBindings(CommandBindings);
      CommandsController.AddInputBindings();
      App.Core.Model.WindowsDisplayScale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;
      MenuViewers.Header = App.Core.Model.CurrentViewer?.Title ?? "Viewer";
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
          AddMenuItem(TreeViewCommands.GeoNameNewCommand);

        if (category.CanModifyItems) {
          var cat = item as BaseCategoryItem;
          var group = item as CategoryGroup;

          if (cat != null || group != null || category.CanHaveSubItems) {
            AddMenuItem(TreeViewCommands.TagItemNewCommand);
            AddMenuItem(TreeViewCommands.TagItemDeleteNotUsedCommand);
          }

          if (item is BaseTreeViewTagItem && group == null || item is Viewer) {
            AddMenuItem(TreeViewCommands.TagItemRenameCommand);
            AddMenuItem(TreeViewCommands.TagItemDeleteCommand);
          }

          if (category.CanHaveGroups && cat != null)
            AddMenuItem(TreeViewCommands.CategoryGroupNewCommand);

          if (group != null) {
            AddMenuItem(TreeViewCommands.CategoryGroupRenameCommand);
            AddMenuItem(TreeViewCommands.CategoryGroupDeleteCommand);
          }
        }
      }

      switch (item) {
        case Folder folder: {
          AddMenuItem(TreeViewCommands.FolderNewCommand);

          if (folder.Parent != null) {
            AddMenuItem(TreeViewCommands.FolderRenameCommand);
            AddMenuItem(TreeViewCommands.FolderDeleteCommand);
            AddMenuItem(TreeViewCommands.FolderAddToFavoritesCommand);
          }

          AddMenuItem(TreeViewCommands.FolderSetAsFolderKeywordCommand);
          AddMenuItem(MetadataCommands.Reload2Command);
          AddMenuItem(MediaItemsCommands.RebuildThumbnailsCommand);
          break;
        }
        case FavoriteFolder _: {
          AddMenuItem(TreeViewCommands.FolderRemoveFromFavoritesCommand);
          break;
        }
        case Viewer _: {
          AddMenuItem(TreeViewCommands.ViewerIncludeFolderCommand);
          AddMenuItem(TreeViewCommands.ViewerExcludeFolderCommand);
          break;
        }
        case Rating _:
        case Person _:
        case Keyword _:
        case GeoName _: {
          AddMenuItem(TreeViewCommands.ActivateFilterAndCommand);
          AddMenuItem(TreeViewCommands.ActivateFilterOrCommand);
          AddMenuItem(TreeViewCommands.ActivateFilterNotCommand);
          break;
        }
        case FolderKeywords _: {
          AddMenuItem(Commands.WindowCommands.OpenFolderKeywordsListCommand);
          break;
        }
        case BaseTreeViewItem bti: {
          if (bti.Parent?.Parent is Viewer)
            AddMenuItem(TreeViewCommands.ViewerRemoveFolderCommand);
          break;
        }
      }

      if (menu.Items.Count > 0)
        stackPanel.ContextMenu = menu;
    }

    #region TreeView
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
        App.Core.MediaItemsViewModel.CopyMove(
          foMode, App.Core.Model.MediaItems.FilteredItems.Where(x => x.IsSelected).ToList(), (Folder) destData);
        App.Core.Model.MediaItems.Helper.IsModified = true;
      }
      // Folder
      else if (e.Data.GetDataPresent(typeof(Folder))) {
        var foMode = e.KeyStates == DragDropKeyStates.ControlKey ? FileOperationMode.Copy : FileOperationMode.Move;
        var srcData = (Folder) e.Data.GetData(typeof(Folder));
        if (srcData == null) return;

        FoldersViewModel.CopyMove(foMode, srcData, (Folder)destData);
        App.Core.Model.MediaItems.Helper.IsModified = true;
        App.Core.Model.Folders.Helper.IsModified = true;
        App.Core.Model.FolderKeywords.Load();

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
        App.Core.Model.Keywords.ItemMove(srcData, destData, dropOnTop);
      }
      // Person
      else if (e.Data.GetDataPresent(typeof(Person))) {
        var srcData = (Person) e.Data.GetData(typeof(Person));
        if (srcData == null) return;
        App.Core.Model.People.ItemMove(srcData, destData);
      }

      App.Core.Model.Sdb.SaveAllTables();
    }

    private void TreeView_AllowDropCheck(object sender, DragEventArgs e) {
      // scroll treeView when the mouse is near the top or bottom
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

      // return if the data can be dropped
      var dataContext = ((StackPanel) sender).DataContext;

      // MediaItems
      if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
        var dragged = ((string[]) e.Data.GetData(DataFormats.FileDrop))?.OrderBy(x => x).ToArray();
        var selected = App.Core.Model.MediaItems.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).OrderBy(p => p).ToArray();

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

      // can't be dropped
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }
    #endregion

    #region Thumbnail
    private void Thumb_OnPreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var mi = (MediaItem) ((FrameworkElement) sender).DataContext;

      // use middle and right button like CTRL + left button
      if (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Right) {
        isCtrlOn = true;
        isShiftOn = false;
      }

      App.Core.Model.MediaItems.Select(isCtrlOn, isShiftOn, mi);
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private void Thumb_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      _dragDropStartPosition = e.GetPosition(null);
      if (e.ClickCount != 2) return;

      var mi = ((FrameworkElement) sender).DataContext as MediaItem;

      if (mi == null) return;
      App.Core.Model.MediaItems.DeselectAll();
      App.Core.Model.MediaItems.Current = mi;

      if (mi.MediaType == MediaType.Video) {
        (VideoThumbnailPreview.Parent as Grid)?.Children.Remove(VideoThumbnailPreview);
        VideoThumbnailPreview.Source = null;
      }

      CommandsController.WindowCommands.SwitchToFullScreen();
      SetMediaItemSource();
    }

    private void Thumb_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      var dob = new DataObject();
      var data = App.Core.Model.MediaItems.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).ToList();
      if (data.Count == 0)
        data.Add(((MediaItem) ((FrameworkElement) sender).DataContext).FilePath);
      dob.SetData(DataFormats.FileDrop, data.ToArray());
      DragDrop.DoDragDrop(this, dob, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void Thumb_OnMouseEnter(object sender, MouseEventArgs e) {
      var mi = ((FrameworkElement) sender).DataContext as MediaItem;
      if (mi == null) return;
      if (mi.MediaType != MediaType.Video) return;

      VideoThumbnailPreview.Source = mi.FilePathUri;
      (VideoThumbnailPreview.Parent as Grid)?.Children.Remove(VideoThumbnailPreview);
      ((MediaItemThumbnail) sender).InsertPlayer(VideoThumbnailPreview);
      VideoThumbnailPreview.Play();
    }

    private void Thumb_OnMouseLeave(object sender, MouseEventArgs e) {
      var mi = ((FrameworkElement) sender).DataContext as MediaItem;
      if (mi == null) return;
      if (mi.MediaType != MediaType.Video) return;

      (VideoThumbnailPreview.Parent as Grid)?.Children.Remove(VideoThumbnailPreview);
      VideoThumbnailPreview.Source = null;
    }

    private void ThumbsBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
      if (e.Delta < 0 && App.Core.Model.ThumbScale < .1) return;
      App.Core.Model.ThumbScale += e.Delta > 0 ? .05 : -.05;
      App.Core.AppInfo.IsThumbInfoVisible = App.Core.Model.ThumbScale > 0.5;
      App.Core.Model.MediaItems.ResetThumbsSize();
      App.Core.MediaItemsViewModel.SplittedItemsReload();
    }

    #endregion

    private void MediaItemSize_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      App.Core.Model.MediaItemSizes.Size.SliderChanged = true;
      App.Core.MediaItemsViewModel.ReapplyFilter();
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    public void SetMediaItemSource(bool decoded = false) {
      var current = App.Core.Model.MediaItems.Current;
      switch (current.MediaType) {
        case MediaType.Image: {
          FullImage.SetSource(current, decoded);
          FullMedia.MediaElement.Source = null;
          FullMedia.IsPlaying = false;
          break;
        }
        case MediaType.Video: {
          /*var isBigger = FullMedia.ActualHeight < current.Height ||
                         FullMedia.ActualWidth < current.Width;
          FullMedia.MediaElement.Stretch = isBigger ? Stretch.Uniform : Stretch.None;*/
          FullMedia.MediaElement.Source = current.FilePathUri;
          FullMedia.IsPlaying = true;
          break;
        }
      }
    }

    private void PanelFullScreen_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount == 2) {
        CommandsController.WindowCommands.SwitchToBrowser();
      }
    }

    private void PanelFullScreen_OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) return;
      if (e.Delta < 0) {
        if (MediaItemsCommands.CanNext())
          CommandsController.MediaItemsCommands.Next();
      }
      else {
        if (MediaItemsCommands.CanPrevious())
          CommandsController.MediaItemsCommands.Previous();
      }
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

    public void SetFlyoutMainTreeViewMargin() {
      var top = App.Core.AppInfo.AppMode == AppMode.Browser ? 30 : 0;
      var bottom = FlyoutMainTreeView.IsPinned ? StatusPanel.ActualHeight : 0;
      FlyoutMainTreeViewMargin = new Thickness(0, top, 0, bottom);
    }

    private void MainSplitter_OnDragDelta(object sender, DragDeltaEventArgs e) {
      FlyoutMainTreeView.Width = GridMain.ColumnDefinitions[0].ActualWidth;
      App.Core.MediaItemsViewModel.SplittedItemsReload();
      App.Core.MediaItemsViewModel.ScrollToCurrent();
    }

    private void MainSplitter_OnDragCompleted(object sender, DragCompletedEventArgs e) {
      MainSplitter_OnDragDelta(null, null);
    }

    private void WMain_OnClosing(object sender, CancelEventArgs e) {
      if (App.Core.Model.MediaItems.ModifiedItems.Count > 0 &&
          MessageDialog.Show("Metadata Edit", "Some Media Items are modified, do you want to save them?", true)) {
        CommandsController.MetadataCommands.Save();
      }
      App.Core.Model.Sdb.SaveAllTables();
    }

    private void WMain_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      _dragDropObject = null;
    }

    private void WMain_OnSizeChanged(object sender, SizeChangedEventArgs e) {
      if (App.Core.AppInfo.AppMode == AppMode.Viewer) return;
      App.Core.MediaItemsViewModel.SplittedItemsReload();
      App.Core.MediaItemsViewModel.ScrollToCurrent();
    }

    private void FiltersPanel_ClearFilters(object sender, MouseButtonEventArgs e) {
      App.Core.ClearFilters();
    }
  }
}
