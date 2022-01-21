using System;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class SegmentPersonChangeEventArgs : EventArgs {
    public SegmentM Segment { get; }
    public PersonM OldPerson { get; }
    public PersonM NewPerson { get; }

    public SegmentPersonChangeEventArgs(SegmentM segment, PersonM oldPerson, PersonM newPerson) {
      Segment = segment;
      OldPerson = oldPerson;
      NewPerson = newPerson;
    }
  }
}
