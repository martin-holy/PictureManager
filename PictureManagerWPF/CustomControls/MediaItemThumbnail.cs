using PictureManager.Commands;
using PictureManager.Domain;
using PictureManager.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PictureManager.ViewModels;

namespace PictureManager.CustomControls {
  public class MediaItemThumbnail : Control {
    private Grid _grid;

    static MediaItemThumbnail() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaItemThumbnail), new FrameworkPropertyMetadata(typeof(MediaItemThumbnail)));
    }

    public override void OnApplyTemplate() {
      _grid = Template.FindName("PART_Grid", this) as Grid;

      if (Template.FindName("PART_Border", this) is Border thumb) {
        thumb.MouseLeftButtonDown += Thumb_OnMouseLeftButtonDown;
        thumb.MouseEnter += Thumb_OnMouseEnter;
        thumb.MouseLeave += Thumb_OnMouseLeave;
        thumb.PreviewMouseUp += Thumb_OnPreviewMouseUp;
      }

      base.OnApplyTemplate();
    }

    private static void Thumb_OnPreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var mi = (MediaItemBaseVM)((FrameworkElement)sender).DataContext;
      App.Core.MediaItemsM.ThumbsGrid.Select(mi.Model, isCtrlOn, isShiftOn);
      App.Ui.MarkUsedKeywordsAndPeople();
    }

    private void Thumb_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount != 2) return;
      if (((FrameworkElement)sender).DataContext is not MediaItemBaseVM mi) return;
      App.Core.MediaItemsM.ThumbsGrid.DeselectAll();
      App.Core.MediaItemsM.ThumbsGrid.Current = mi.Model;

      if (mi.Model.MediaType == MediaType.Video) {
        (App.WMain.VideoThumbnailPreview.Parent as Grid)?.Children.Remove(App.WMain.VideoThumbnailPreview);
        App.WMain.VideoThumbnailPreview.Source = null;
      }

      WindowCommands.SwitchToFullScreen();
      App.WMain.MediaViewer.SetMediaItems(App.Ui.MediaItemsBaseVM.ToViewModel(App.Core.MediaItemsM.ThumbsGrid.FilteredItems).ToList());
      App.WMain.MediaViewer.SetMediaItemSource(mi);
    }

    private void Thumb_OnMouseEnter(object sender, MouseEventArgs e) {
      if (((FrameworkElement)sender).DataContext is not MediaItemBaseVM mi) return;
      if (mi.Model.MediaType != MediaType.Video) return;

      var player = App.WMain.VideoThumbnailPreview;
      var rotation = new TransformGroup();
      rotation.Children.Add(new RotateTransform(mi.Model.RotationAngle));
      (player.Parent as Grid)?.Children.Remove(player);
      player.LayoutTransform = rotation;
      player.Source = new(mi.Model.FilePath);
      _grid.Children.Insert(2, player);
      player.Play();
    }

    public void InsertPlayer(UIElement player) => _grid.Children.Insert(2, player);

    private void Thumb_OnMouseLeave(object sender, MouseEventArgs e) {
      if (((FrameworkElement)sender).DataContext is not MediaItemBaseVM mi) return;
      if (mi.Model.MediaType != MediaType.Video) return;

      (App.WMain.VideoThumbnailPreview.Parent as Grid)?.Children.Remove(App.WMain.VideoThumbnailPreview);
      App.WMain.VideoThumbnailPreview.Source = null;
    }
  }
}
