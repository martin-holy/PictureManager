using System;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class SegmentPersonChangedEventArgs : EventArgs {
    public SegmentM Segment { get; }

    public SegmentPersonChangedEventArgs(SegmentM segment) {
      Segment = segment;
    }
  }
}
