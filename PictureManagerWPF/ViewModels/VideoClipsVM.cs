using System;
using System.IO;
using System.Windows;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;

namespace PictureManager.ViewModels {
  public sealed class VideoClipsVM : ObservableObject {
    private VideoPlayer _videoPlayer;

    public VideoClipsM Model { get; }
    public readonly HeaderedListItem<object, string> ToolsTabsItem;
    public bool ShowRepeatSlider => VideoPlayer?.PlayType is PlayType.Clips or PlayType.Group;

    public VideoPlayer VideoPlayer {
      get => _videoPlayer;
      set {
        _videoPlayer = value;
        _videoPlayer.SelectNextClip = Model.SelectNext;

        _videoPlayer.PropertyChanged += (_, e) => {
          if (nameof(_videoPlayer.PlayType).Equals(e.PropertyName))
            OnPropertyChanged(nameof(ShowRepeatSlider));
        };
      }
    }

    public RelayCommand<bool> SetMarkerCommand { get; }
    public RelayCommand<PlayType> SetPlayTypeCommand { get; }
    public RelayCommand<VideoClipM> SetCurrentVideoClipCommand { get; }
    public RelayCommand<object> SplitCommand { get; }
    public RelayCommand<object> SaveCommand { get; }
    public RelayCommand<int> SeekToPositionCommand { get; }

    public VideoClipsVM(VideoClipsM model) {
      Model = model;
      ToolsTabsItem = new(this, "Clips");
      
      Model.ItemCreatedEventHandler += (_, e) => {
        SetMarker(true);
        Model.ScrollToItem = e.Data;
      };

      SetMarkerCommand = new(
        SetMarker,
        () => model.CurrentVideoClip != null);

      SetPlayTypeCommand = new(
        pt => VideoPlayer.PlayType = pt);
      
      SetCurrentVideoClipCommand = new(
        SetCurrentVideoClip,
        item => item != null);
      
      SplitCommand = new(
        VideoClipSplit,
        () => VideoPlayer?.Player?.Source != null);
      
      SaveCommand = new(
        () => {
          Model.DataAdapter.Save();
          Model.GroupsM.DataAdapter.Save();
        },
        () =>
          Model.DataAdapter.IsModified ||
          Model.GroupsM.DataAdapter.IsModified
      );

      SeekToPositionCommand = new(position => {
        VideoPlayer.TimelinePosition = position;
      });
    }

    private void SetMarker(bool start) {
      var vc = Model.CurrentVideoClip;

      Model.SetMarker(
        vc,
        start,
        (int)Math.Round(VideoPlayer.TimelinePosition),
        VideoPlayer.Volume,
        VideoPlayer.Speed);

      VideoPlayer.ClipTimeStart = vc.TimeStart;
      VideoPlayer.ClipTimeEnd = vc.TimeEnd;

      if (start)
        CreateThumbnail(
          vc,
          VideoPlayer.Player,
          true);
    }

    private void VideoClipSplit() {
      if (Model.CurrentVideoClip?.TimeEnd == 0)
        SetMarker(false);
      else
        Model.ItemCreate(Model);
    }

    private void SetCurrentVideoClip(VideoClipM vc) {
      Model.CurrentVideoClip = vc;
      VideoPlayer.ClipTimeStart = vc.TimeStart;
      VideoPlayer.ClipTimeEnd = vc.TimeEnd;

      if (VideoPlayer.PlayType != PlayType.Video) {
        VideoPlayer.Volume = vc.Volume;
        VideoPlayer.Speed = vc.Speed;
      }

      if (VideoPlayer.IsPlaying)
        VideoPlayer.StartClipTimer();
    }

    private static void CreateThumbnail(VideoClipM vc, FrameworkElement visual, bool reCreate = false) {
      if (File.Exists(vc.ThumbPath) && !reCreate) return;

      Imaging.CreateVideoThumbnailFromVisual(
        visual,
        vc.ThumbPath,
        Settings.Default.ThumbnailSize,
        Settings.Default.JpegQualityLevel);

      vc.OnPropertyChanged(nameof(vc.ThumbPath));
    }
  }
}
