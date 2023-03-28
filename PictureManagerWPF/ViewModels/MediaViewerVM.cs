using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using MediaPlayer = MH.UI.WPF.Controls.MediaPlayer;

namespace PictureManager.ViewModels {
  public sealed class MediaViewerVM : ObservableObject {
    private BitmapImage _imageSource;

    public AppCore CoreVM { get; }
    public MediaViewerM Model { get; }
    public BitmapImage ImageSource { get => _imageSource; set { _imageSource = value; OnPropertyChanged(); } }
    public MediaPlayer FullVideo { get; private set; }

    public RelayCommand<MouseWheelEventArgs> NavigateCommand { get; }
    public RelayCommand<RoutedEventArgs> MediaPlayerLoadedCommand { get; }

    public MediaViewerVM(AppCore coreVM, MediaViewerM model) {
      CoreVM = coreVM;
      Model = model;
      
      NavigateCommand = new(Navigate);
      MediaPlayerLoadedCommand = new(e => FullVideo = e.Source as MediaPlayer);

      AttachEvents();
    }

    private void AttachEvents() {
      Model.PropertyChanged += (_, e) => {
        if (nameof(Model.Current).Equals(e.PropertyName))
          SetMediaItemSource(Model.Current);
      };
    }

    public void SetMediaItemSource(MediaItemM mediaItem) {
      if (mediaItem == null || mediaItem.MediaType != MediaType.Image)
        ImageSource = null;

      if (mediaItem == null || mediaItem.MediaType != MediaType.Video) {
        App.Core.VideoClipsM.SetMediaItem(null);
        Model.MediaPlayerM.IsPlaying = false;
        Model.MediaPlayerM.Source = String.Empty;
        App.Ui.ToolsTabsVM.Deactivate(App.Ui.VideoClipsVM.ToolsTabsItem);
      }

      if (mediaItem == null) return;

      switch (mediaItem.MediaType) {
        case MediaType.Image: {
          ImageSource = Imaging.GetBitmapImage(mediaItem.FilePath, (MediaOrientation)mediaItem.Orientation);
          break;
        }
        case MediaType.Video: {
          var data = ShellStuff.FileInformation.GetVideoMetadata(mediaItem.Folder.FullPath, mediaItem.FileName);
          var fps = (double)data[3] > 0 ? (double)data[3] : 30.0;
          var smallChange = Math.Round(1000 / fps, 0);

          App.Core.VideoClipsM.SetMediaItem(mediaItem);
          Model.MediaPlayerM.Source = mediaItem.FilePath;
          Model.MediaPlayerM.TimelineSmallChange = smallChange;
          App.Ui.ToolsTabsVM.Activate(App.Ui.VideoClipsVM.ToolsTabsItem);
          break;
        }
      }
    }

    private void Navigate(MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) return;
      if (e.Delta < 0) {
        if (Model.CanNext())
          Model.Next();
      }
      else {
        if (Model.CanPrevious())
          Model.Previous();
      }
    }
  }
}
