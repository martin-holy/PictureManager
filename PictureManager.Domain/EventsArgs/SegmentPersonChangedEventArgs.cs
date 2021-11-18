using System;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class SegmentPersonChangedEventArgs : EventArgs {
    public Segment Segment { get; }

    public SegmentPersonChangedEventArgs(Segment segment) {
      Segment = segment;
    }
  }
}
