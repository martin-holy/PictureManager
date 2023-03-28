using System;
using System.IO;
using System.Windows;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Properties;

namespace PictureManager.ViewModels {
  public sealed class VideoClipsVM : ObservableObject {
    public VideoClipsM Model { get; }
    public readonly HeaderedListItem<object, string> ToolsTabsItem;

    public VideoClipsVM(VideoClipsM model) {
      Model = model;
      ToolsTabsItem = new(this, "Clips");

      Model.CreateThumbnail = CreateThumbnail;
    }

    private void CreateThumbnail(VideoClipM vc, bool reCreate = false) =>
      CreateThumbnail(vc, App.Ui.MediaViewerVM.FullVideo, reCreate);

    private static void CreateThumbnail(VideoClipM vc, FrameworkElement visual, bool reCreate = false) {
      if (File.Exists(vc.ThumbPath) && !reCreate) return;

      Utils.Imaging.CreateThumbnailFromVisual(
        visual,
        vc.ThumbPath,
        Settings.Default.ThumbnailSize,
        Settings.Default.JpegQualityLevel);

      vc.OnPropertyChanged(nameof(vc.ThumbPath));
    }
  }
}
