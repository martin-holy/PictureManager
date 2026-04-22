using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Controls.Hosts.ZoomAndPanHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.MediaItem.Video;
using System.Threading;

namespace PictureManager.Android.Views.Sections;

public sealed class VideoDetailV : LinearLayout {
  private readonly VideoVM _videoVM;

  private readonly ZoomAndPanHost _zoomAndPanHost;
  private readonly ZoomableVideoView _video;
  private readonly MediaPlayerControlPanel _controlPanel;
  private readonly CollectionViewHost _videoItems;

  private CancellationTokenSource? _cts;

  public VideoDetailV(Context context, VideoVM videoVM) : base(context) {
    _videoVM = videoVM;
    Orientation = Orientation.Vertical;

    // TODO maybe I should have this ZoomAndPan instance in VideoVM
    _zoomAndPanHost = new(context, new ZoomAndPan() { ExpandToFill = true });
    _video = new(context, _zoomAndPanHost.DataContext, (AndroidMediaPlayer)videoVM.UiDetailVideo) { AutoHeightFromAspectRatio = true };
    _controlPanel = new(context, videoVM.MediaPlayer, new RelayCommand(_play, Res.IconPlay, "Play"));
    _videoItems = new(context, videoVM.CurrentVideoItems, CreateVideoItemV);

    var video = new FrameLayout(context)
      .Add(_zoomAndPanHost, LPU.FrameMatch())
      .Add(_video, LPU.FrameMatch());

    AddView(video, LPU.LinearMatchWrap());
    AddView(_controlPanel, LPU.Linear(LPU.Match, LPU.Wrap, GravityFlags.CenterHorizontal));
    AddView(_videoItems, LPU.Linear(LPU.Match, 0, 1));

    _video.PlayRequested += videoVM.PlayInDetailView;

    videoVM.Bind(nameof(VideoVM.Current), x => x.Current, _onCurrentVideoChanged);
  }

  public void Activate() {
    if (_videoVM.MediaPlayer.IsPlaying)
      _video.RequestPlay();
  }

  private void _onCurrentVideoChanged(VideoM? current) {
    _cts?.Cancel();

    if (current == null) {
      _video.Clear();
      return;
    }

    var (width, height) = current.GetRotatedSize();
    _zoomAndPanHost.DataContext.SetContentSize(width, height);
    _cts = new CancellationTokenSource();
    _ = _video.SetPath(current.FilePath, current.Orientation, Context!, _cts.Token);
  }

  private void _play() {
    if (_video.PreviewOnly)
      _video.RequestPlay();
    else
      _videoVM.MediaPlayer.IsPlaying = true;
  }

  public static VideoItemV CreateVideoItemV(Context context, CollectionView.ViewMode viewMode) =>
    new(context);

  protected override void Dispose(bool disposing) {
    if (disposing) {
      _cts?.Dispose();
      _cts = null;
    }
    base.Dispose(disposing);
  }
}