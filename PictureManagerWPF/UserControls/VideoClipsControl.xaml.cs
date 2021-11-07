using System;
using System.Collections.ObjectModel;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using PictureManager.CustomControls;
using PictureManager.ViewModels.Tree;

namespace PictureManager.UserControls {
  public partial class VideoClipsControl {
    public VideoPlayer VideoPlayer { get; set; }
    public ObservableCollection<ICatTreeViewCategory> MediaItemClips { get; } = new() { App.Ui.VideoClipsTreeVM };
    public RelayCommand<bool> SetMarkerCommand { get; }
    public RelayCommand<PlayType> SetPlayTypeCommand { get; }
    public RelayCommand<VideoClipTreeVM> SetCurrentVideoClipCommand { get; }
    public RelayCommand<object> VideoClipSplitCommand { get; }
    public RelayCommand<int> SeekToPositionCommand { get; }

    public VideoClipsControl() {
      // commands needs to be created before InitializeComponent
      SetMarkerCommand = new(SetMarker);
      SetPlayTypeCommand = new(pt => VideoPlayer.PlayType = pt);
      SetCurrentVideoClipCommand = new(SetCurrentVideoClip, vc => vc != null);
      VideoClipSplitCommand = new(VideoClipSplit, () => VideoPlayer?.Player?.Source != null);
      SeekToPositionCommand = new(position => {
        VideoPlayer.TimelineSlider.Value = position;
      });

      InitializeComponent();
    }

    private void SetMarker(bool start) {
      var vcm = VideoPlayer.CurrentVideoClip.Model;
      App.Core.VideoClipsM.SetMarker(vcm, start, (int)Math.Round(VideoPlayer.TimelineSlider.Value), VideoPlayer.VolumeSlider.Value, VideoPlayer.SpeedSlider.Value);
      if (start) VideoClipsTreeVM.CreateThumbnail(vcm, VideoPlayer.Player, true);
    }

    private void VideoClipSplit() {
      if (VideoPlayer.CurrentVideoClip?.Model.TimeEnd == 0)
        SetMarker(false);
      else
        App.Ui.VideoClipsTreeVM.ItemCreate(App.Ui.VideoClipsTreeVM);
    }

    private void SetCurrentVideoClip(VideoClipTreeVM vc) {
      VideoPlayer.CurrentVideoClip = vc;
      if (VideoPlayer.PlayType != PlayType.Video) {
        VideoPlayer.VolumeSlider.Value = vc.Model.Volume;
        VideoPlayer.SpeedSlider.Value = vc.Model.Speed;
      }

      if (VideoPlayer.IsPlaying)
        VideoPlayer.StartClipTimer();
    }
  }
}
