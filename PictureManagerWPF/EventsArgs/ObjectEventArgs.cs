using System;

namespace PictureManager.EventsArgs {
  public class ObjectEventArgs : EventArgs {
    public object Data { get; }

    public ObjectEventArgs(object data) {
      Data = data;
    }
  }
}
