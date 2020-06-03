using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.CustomControls {
  public class MediaItemThumbnail: Control {
    private Point _dragDropStartPosition;
    private Grid _grid;

    static MediaItemThumbnail() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaItemThumbnail),
        new FrameworkPropertyMetadata(typeof(MediaItemThumbnail)));
    }

    public override void OnApplyTemplate() {
      _grid = Template.FindName("PART_Grid", this) as Grid;

      if (Template.FindName("PART_Border", this) is Border thumb) {
        thumb.MouseLeftButtonDown += Thumb_OnMouseLeftButtonDown;
        thumb.MouseMove += Thumb_OnMouseMove;
        thumb.MouseEnter += Thumb_OnMouseEnter;
        thumb.MouseLeave += Thumb_OnMouseLeave;
        thumb.PreviewMouseUp += Thumb_OnPreviewMouseUp;
      }

      base.OnApplyTemplate();
    }

    private static void Thumb_OnPreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var mi = (MediaItem)((FrameworkElement)sender).DataContext;

      // use middle and right button like CTRL + left button
      if (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Right) {
        isCtrlOn = true;
        isShiftOn = false;
      }

      App.Core.Model.MediaItems.ThumbsGrid.Select(isCtrlOn, isShiftOn, mi);
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private void Thumb_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      _dragDropStartPosition = e.GetPosition(null);
      if (e.ClickCount != 2) return;

      var mi = ((FrameworkElement)sender).DataContext as MediaItem;

      if (mi == null) return;
      App.Core.Model.MediaItems.ThumbsGrid.DeselectAll();
      App.Core.Model.MediaItems.ThumbsGrid.Current = mi;

      if (mi.MediaType == MediaType.Video) {
        (App.WMain.VideoThumbnailPreview.Parent as Grid)?.Children.Remove(App.WMain.VideoThumbnailPreview);
        App.WMain.VideoThumbnailPreview.Source = null;
      }

      App.WMain.CommandsController.WindowCommands.SwitchToFullScreen();
      App.WMain.SetMediaItemSource();
    }

    private void Thumb_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      var dob = new DataObject();
      var data = App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).ToList();
      if (data.Count == 0)
        data.Add(((MediaItem)((FrameworkElement)sender).DataContext).FilePath);
      dob.SetData(DataFormats.FileDrop, data.ToArray());
      DragDrop.DoDragDrop(this, dob, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void Thumb_OnMouseEnter(object sender, MouseEventArgs e) {
      var mi = ((FrameworkElement)sender).DataContext as MediaItem;
      if (mi == null) return;
      if (mi.MediaType != MediaType.Video) return;

      (App.WMain.VideoThumbnailPreview.Parent as Grid)?.Children.Remove(App.WMain.VideoThumbnailPreview);
      App.WMain.VideoThumbnailPreview.Source = mi.FilePathUri;
      _grid.Children.Insert(2, App.WMain.VideoThumbnailPreview);
      App.WMain.VideoThumbnailPreview.Play();
    }

    public void InsertPlayer(UIElement player) {
      _grid.Children.Insert(2, player);
    }

    private void Thumb_OnMouseLeave(object sender, MouseEventArgs e) {
      var mi = ((FrameworkElement)sender).DataContext as MediaItem;
      if (mi == null) return;
      if (mi.MediaType != MediaType.Video) return;

      (App.WMain.VideoThumbnailPreview.Parent as Grid)?.Children.Remove(App.WMain.VideoThumbnailPreview);
      App.WMain.VideoThumbnailPreview.Source = null;
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }
  }
}
