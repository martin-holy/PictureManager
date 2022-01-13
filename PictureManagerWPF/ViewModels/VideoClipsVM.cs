using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Interfaces;
using PictureManager.CustomControls;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;
using PictureManager.ViewModels.Tree;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public sealed class VideoClipsVM : ObservableObject {
    private VideoPlayer _videoPlayer;
    private readonly VideoClipsM _model;
    private readonly VideoClipsTreeVM _treeVM;

    public CatTreeView CtvClips { get; set; }
    public ObservableCollection<ICatTreeViewCategory> MediaItemClips { get; }
    public bool ShowRepeatSlider => VideoPlayer?.PlayType is PlayType.Clips or PlayType.Group;

    public VideoPlayer VideoPlayer {
      get => _videoPlayer;
      set {
        _videoPlayer = value;
        _videoPlayer.SelectNextClip = SelectNext;

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

    public VideoClipsVM(VideoClipsM model, VideoClipsTreeVM treeVM) {
      _model = model;
      _treeVM = treeVM;
      MediaItemClips = new() { _treeVM };

      _treeVM.ItemCreatedEventHandler += (_, e) => {
        SetMarker(true);
        CtvClips.ScrollTo((VideoClipTreeVM)e.Data);
      };

      SetMarkerCommand = new(
        SetMarker,
        () => model.CurrentVideoClip != null);

      SetPlayTypeCommand = new(
        pt => VideoPlayer.PlayType = pt);
      
      SetCurrentVideoClipCommand = new(
        item => SetCurrentVideoClip(((VideoClipTreeVM)item).Model),
        item => item is VideoClipTreeVM);
      
      SplitCommand = new(
        VideoClipSplit,
        () => VideoPlayer?.Player?.Source != null);
      
      SaveCommand = new(
        () => {
          _model.DataAdapter.Save();
          _model.GroupsM.DataAdapter.Save();
        },
        () =>
          _model.DataAdapter.IsModified ||
          _model.GroupsM.DataAdapter.IsModified
      );

      SeekToPositionCommand = new(position => {
        VideoPlayer.TimelinePosition = position;
      });
    }

    private void SetMarker(bool start) {
      var vc = _model.CurrentVideoClip;

      _model.SetMarker(
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
      if (_model.CurrentVideoClip?.TimeEnd == 0)
        SetMarker(false);
      else
        _treeVM.ItemCreate(_treeVM);
    }

    private void SetCurrentVideoClip(VideoClipM vc) {
      _model.CurrentVideoClip = vc;
      VideoPlayer.ClipTimeStart = vc.TimeStart;
      VideoPlayer.ClipTimeEnd = vc.TimeEnd;

      if (VideoPlayer.PlayType != PlayType.Video) {
        VideoPlayer.Volume = vc.Volume;
        VideoPlayer.Speed = vc.Speed;
      }

      if (VideoPlayer.IsPlaying)
        VideoPlayer.StartClipTimer();
    }

    private void SelectNext(bool inGroup, bool selectFirst) {
      var clip = _model.GetNextClip(inGroup, selectFirst);
      if (clip == null) return;
      var clipTreeVM = _treeVM.All[clip.Id];
      if (Equals(clip, _model.CurrentVideoClip))
        clipTreeVM.IsSelected = false;
      clipTreeVM.IsSelected = true;
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
