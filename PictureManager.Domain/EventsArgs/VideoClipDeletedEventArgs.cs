using System;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class VideoClipDeletedEventArgs : EventArgs {
    public VideoClipM VideoClip { get; }

    public VideoClipDeletedEventArgs(VideoClipM videoClip) {
      VideoClip = videoClip;
    }
  }
}
