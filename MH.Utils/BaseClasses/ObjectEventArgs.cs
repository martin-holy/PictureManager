using System;

namespace MH.Utils.BaseClasses {
  public class ObjectEventArgs : EventArgs {
    public object Data { get; }

    public ObjectEventArgs(object data) {
      Data = data;
    }
  }
}
