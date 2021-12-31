using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Interfaces;
using PictureManager.ViewModels.Tree;

namespace PictureManager.UserControls {
  public partial class VideoClipsControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged(this, new(name));

    private VideoPlayer _videoPlayer;

    public ObservableCollection<ICatTreeViewCategory> MediaItemClips { get; } = new() { App.Ui.VideoClipsTreeVM };
    public bool ShowRepeatSlider => VideoPlayer?.PlayType is PlayType.Clips or PlayType.Group;

    public VideoPlayer VideoPlayer {
      get => _videoPlayer;
      set {
        _videoPlayer = value;
        _videoPlayer.SelectNextClip = App.Ui.VideoClipsTreeVM.SelectNext;

        _videoPlayer.PropertyChanged += (_, e) => {
          if (nameof(_videoPlayer.PlayType).Equals(e.PropertyName))
            OnPropertyChanged(nameof(ShowRepeatSlider));
        };
      }
    }

    public RelayCommand<bool> SetMarkerCommand { get; }
    public RelayCommand<PlayType> SetPlayTypeCommand { get; }
    public RelayCommand<CatTreeViewItem> SetCurrentVideoClipCommand { get; }
    public RelayCommand<object> SplitCommand { get; }
    public RelayCommand<object> SaveCommand { get; }
    public RelayCommand<int> SeekToPositionCommand { get; }

    public VideoClipsControl() {
      // commands needs to be created before InitializeComponent
      SetMarkerCommand = new(SetMarker);
      SetPlayTypeCommand = new(pt => VideoPlayer.PlayType = pt);
      SetCurrentVideoClipCommand = new(item => SetCurrentVideoClip(item as VideoClipTreeVM), item => item is VideoClipTreeVM);
      SplitCommand = new(VideoClipSplit, () => VideoPlayer?.Player?.Source != null);
      
      SaveCommand = new(
        () => {
          App.Core.VideoClipsM.DataAdapter.Save();
          App.Core.VideoClipsGroupsM.DataAdapter.Save();
        },
        () =>
          App.Core.VideoClipsM.DataAdapter.IsModified ||
          App.Core.VideoClipsGroupsM.DataAdapter.IsModified
      );

      SeekToPositionCommand = new(position => {
        VideoPlayer.TimelinePosition = position;
      });

      InitializeComponent();
    }

    private void SetMarker(bool start) {
      var vcm = App.Ui.VideoClipsTreeVM.CurrentVideoClip.Model;
      App.Core.VideoClipsM.SetMarker(vcm, start, (int)Math.Round(VideoPlayer.TimelinePosition), VideoPlayer.Volume, VideoPlayer.Speed);
      if (start) VideoClipsTreeVM.CreateThumbnail(vcm, VideoPlayer.Player, true);
    }

    private void VideoClipSplit() {
      if (App.Ui.VideoClipsTreeVM.CurrentVideoClip?.Model.TimeEnd == 0)
        SetMarker(false);
      else
        App.Ui.VideoClipsTreeVM.ItemCreate(App.Ui.VideoClipsTreeVM);
    }

    private void SetCurrentVideoClip(VideoClipTreeVM vc) {
      App.Ui.VideoClipsTreeVM.CurrentVideoClip = vc;
      VideoPlayer.ClipTimeStart = vc.Model.TimeStart;
      VideoPlayer.ClipTimeEnd = vc.Model.TimeEnd;

      if (VideoPlayer.PlayType != PlayType.Video) {
        VideoPlayer.Volume = vc.Model.Volume;
        VideoPlayer.Speed = vc.Model.Speed;
      }

      if (VideoPlayer.IsPlaying)
        VideoPlayer.StartClipTimer();
    }
  }
}
