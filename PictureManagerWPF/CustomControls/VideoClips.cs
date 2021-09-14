using PictureManager.Domain.CatTreeViewModels;
using PictureManager.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace PictureManager.CustomControls {
  public class VideoClips : Control {
    public VideoPlayer VideoPlayer { get; set; }
    public CatTreeView CtvClips { get; private set; }
    public ObservableCollection<ICatTreeViewCategory> MediaItemClips { get; set; }

    static VideoClips() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(VideoClips), new FrameworkPropertyMetadata(typeof(VideoClips)));
    }

    public VideoClips() {
      MediaItemClips = new ObservableCollection<ICatTreeViewCategory> {
        App.Ui.MediaItemClipsCategory
      };
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      if (Template.FindName("PART_BtnMarkerA", this) is Button btnMarkerA)
        btnMarkerA.Click += (o, e) => VideoPlayer.SetMarker(VideoPlayer.CurrentVideoClip, true);

      if (Template.FindName("PART_BtnMarkerB", this) is Button btnMarkerB)
        btnMarkerB.Click += (o, e) => VideoPlayer.SetMarker(VideoPlayer.CurrentVideoClip, false);

      if (Template.FindName("PART_SpPlayTypes", this) is StackPanel spPlayTypes)
        spPlayTypes.PreviewMouseLeftButtonUp += (o, e) => {
          if (e.OriginalSource is RadioButton rb) {
            VideoPlayer.PlayType = (PlayType)rb.Tag;
          }
        };

      if (Template.FindName("PART_CtvClips", this) is CatTreeView ctvClips) {
        CtvClips = ctvClips;

        CtvClips.SelectedItemChanged += (o, e) => {
          VideoPlayer.CurrentVideoClip = CtvClips.SelectedItem as VideoClipViewModel;
          if (VideoPlayer.CurrentVideoClip != null && VideoPlayer.PlayType != PlayType.Video) {
            VideoPlayer.VolumeSlider.Value = VideoPlayer.CurrentVideoClip.Clip.Volume;
            VideoPlayer.SpeedSlider.Value = VideoPlayer.CurrentVideoClip.Clip.Speed;
          }

          if (VideoPlayer.IsPlaying) VideoPlayer.StartClipTimer();
        };

        CtvClips.PreviewMouseLeftButtonUp += (o, e) => {
          if (e.OriginalSource is FrameworkElement fe && CtvClips.SelectedItem is VideoClipViewModel vc) {
            switch (fe.Name) {
              case "TbMarkerA":
              // Seek to start video position defined in Clip
              VideoPlayer.TimelineSlider.Value = vc.Clip.TimeStart;
              break;

              case "TbMarkerB":
              // Seek to end video position defined in Clip
              VideoPlayer.TimelineSlider.Value = vc.Clip.TimeEnd;
              break;
            }
          }
        };
      }
    }
  }
}
