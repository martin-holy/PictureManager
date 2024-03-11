using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Models.MediaItems;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace PictureManager.Windows.WPF.ViewModels {
  public static class MediaItemVideoPreviewVM {
    public static MediaElement VideoPreview { get; }
    public static RelayCommand<Grid> ShowVideoPreviewCommand { get; }
    public static RelayCommand HideVideoPreviewCommand { get; }

    static MediaItemVideoPreviewVM() {
      ShowVideoPreviewCommand = new(ShowVideoPreview);
      HideVideoPreviewCommand = new(HideVideoPreview);

      VideoPreview = new() {
        LoadedBehavior = MediaState.Manual,
        IsMuted = true,
        Stretch = Stretch.Fill
      };

      VideoPreview.MediaEnded += (o, _) => {
        // MediaElement.Stop()/Play() doesn't work when is video shorter than 1s
        ((MediaElement)o).Position = TimeSpan.FromMilliseconds(1);
      };
    }

    private static void ShowVideoPreview(Grid grid) {
      if (grid?.DataContext is not VideoM mi) return;

      var rotation = new TransformGroup();
      rotation.Children.Add(new RotateTransform(mi.Orientation.ToAngle()));
      (VideoPreview.Parent as Grid)?.Children.Remove(VideoPreview);
      VideoPreview.LayoutTransform = rotation;
      VideoPreview.Source = new(mi.FilePath);
      grid.Children.Insert(2, VideoPreview);
      VideoPreview.Play();
    }

    private static void HideVideoPreview() {
      if (VideoPreview.Source == null) return;

      (VideoPreview.Parent as Grid)?.Children.Remove(VideoPreview);
      VideoPreview.Source = null;
    }
  }
}
