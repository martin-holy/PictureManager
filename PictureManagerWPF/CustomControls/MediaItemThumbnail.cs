using PictureManager.Commands;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PictureManager.CustomControls {
  public class MediaItemThumbnail : Control {
    private Point _dragDropStartPosition;
    private Grid _grid;

    static MediaItemThumbnail() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaItemThumbnail), new FrameworkPropertyMetadata(typeof(MediaItemThumbnail)));
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
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var mi = (MediaItem)((FrameworkElement)sender).DataContext;
      App.Core.MediaItems.ThumbsGrid.Select(mi, isCtrlOn, isShiftOn);
      App.Core.MarkUsedKeywordsAndPeople();
      App.WMain.StatusPanel.OnPropertyChanged(nameof(App.WMain.StatusPanel.FileSize));
    }

    private void Thumb_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      _dragDropStartPosition = e.GetPosition(null);
      if (e.ClickCount != 2) return;

      var mi = ((FrameworkElement)sender).DataContext as MediaItem;

      if (mi == null) return;
      App.Core.MediaItems.ThumbsGrid.DeselectAll();
      App.Core.MediaItems.ThumbsGrid.Current = mi;

      if (mi.MediaType == MediaType.Video) {
        (App.WMain.VideoThumbnailPreview.Parent as Grid)?.Children.Remove(App.WMain.VideoThumbnailPreview);
        App.WMain.VideoThumbnailPreview.Source = null;
      }

      WindowCommands.SwitchToFullScreen();
      App.WMain.MediaViewer.SetMediaItems(App.Core.MediaItems.ThumbsGrid.FilteredItems.ToList());
      App.WMain.MediaViewer.SetMediaItemSource(mi);
    }

    private void Thumb_OnMouseMove(object sender, MouseEventArgs e) {
      if (!IsDragDropStarted(e)) return;
      var dob = new DataObject();
      var data = App.Core.MediaItems.ThumbsGrid.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).ToList();
      if (data.Count == 0)
        data.Add(((MediaItem)((FrameworkElement)sender).DataContext).FilePath);
      dob.SetData(DataFormats.FileDrop, data.ToArray());
      DragDrop.DoDragDrop(this, dob, DragDropEffects.Move | DragDropEffects.Copy);
    }

    private void Thumb_OnMouseEnter(object sender, MouseEventArgs e) {
      var mi = ((FrameworkElement)sender).DataContext as MediaItem;
      if (mi == null) return;
      if (mi.MediaType != MediaType.Video) return;

      var player = App.WMain.VideoThumbnailPreview;
      var rotation = new TransformGroup();
      rotation.Children.Add(new RotateTransform(mi.RotationAngle));
      (player.Parent as Grid)?.Children.Remove(player);
      player.LayoutTransform = rotation;
      player.Source = mi.FilePathUri;
      _grid.Children.Insert(2, player);
      player.Play();
    }

    public void InsertPlayer(UIElement player) => _grid.Children.Insert(2, player);

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
