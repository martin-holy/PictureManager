using System;

namespace MH.Utils.BaseClasses {
  public class ObjectEventArgs<T> : EventArgs {
    public T Data { get; }

    public ObjectEventArgs(T data) {
      Data = data;
    }
  }
}